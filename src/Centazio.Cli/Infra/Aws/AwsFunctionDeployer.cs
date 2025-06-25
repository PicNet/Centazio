using System.Text;
using Amazon;
using Amazon.ECR;
using Amazon.ECR.Model;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.Runtime;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Centazio.Core.Runner;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Hosts.Aws;
using Microsoft.Extensions.DependencyInjection;
using AddPermissionRequest = Amazon.Lambda.Model.AddPermissionRequest;
using ResourceNotFoundException = Amazon.Lambda.Model.ResourceNotFoundException;
using Docker.DotNet;
using Docker.DotNet.Models;
using ICSharpCode.SharpZipLib.Tar;

namespace Centazio.Cli.Infra.Aws;

public interface IAwsFunctionDeployer {
  Task Deploy(AwsFunctionProjectMeta project);
}

public class AwsFunctionDeployer([FromKeyedServices(CentazioConstants.Hosts.Aws)] CentazioSettings settings, [FromKeyedServices(CentazioConstants.Hosts.Aws)] CentazioSecrets secrets, ITemplater templater) : IAwsFunctionDeployer {

  public async Task Deploy(AwsFunctionProjectMeta project) =>
      await new AwsFunctionDeployerImpl(settings, new(secrets.AWS_KEY, secrets.AWS_SECRET), project, templater).DeployImpl();
}

// todo CP: use `Centazio.Cli.Infra.AzFunctionProjectMeta` pattern to replace hardcoded values below (names, timeouts, attributes, etc)
internal class AwsFunctionDeployerImpl(CentazioSettings settings, BasicAWSCredentials credentials, AwsFunctionProjectMeta project, ITemplater templater) {

  private readonly ICommandRunner cmd = new CommandRunner();
  private readonly RegionEndpoint region = RegionEndpoint.GetBySystemName(settings.AwsSettings.Region);

  public async Task DeployImpl() {
    if (!Directory.Exists(project.SolutionDirPath)) throw new Exception($"project [{project.ProjectName}] could not be found in the [{settings.Defaults.GeneratedCodeFolder}] folder");
    if (!File.Exists(project.SlnFilePath)) throw new Exception($"project [{project.ProjectName}] does not appear to be a valid as no sln file was found");
    
    Log.Information($"project [{project.ProjectName}] start building and deploying");

    var projnm = project.ProjectName.ToLower();
    using var ecr = new AmazonECRClient(credentials, region);
    var accid = await GetAccountId();

    await CheckAndCreateEcrRepository(ecr, projnm);
    
    var ecruri = $"{accid}.dkr.ecr.{region.SystemName}.amazonaws.com";
    await BuildAndPushDockerImage(ecr, ecruri, projnm);
    
    using var lambda = new AmazonLambdaClient(credentials, region);
    var funcarn = await UpdateOrCreateLambdaFunction(lambda, ecruri, projnm, ecr, accid);
    await SetUpTimer(lambda, funcarn);
  }

  private async Task<string> UpdateOrCreateLambdaFunction(AmazonLambdaClient lambda, string ecruri, string projnm, AmazonECRClient ecr, string accid) {
    var imguri = $"{ecruri}/{projnm}@{await GetLatestImageDigest(ecr, projnm)}";
    return await FunctionExists(lambda, project.AwsFunctionName) ? await UpdateFunction(lambda, imguri) : await CreateFunction(lambda, imguri, accid);
  }

  private async Task BuildAndPushDockerImage(AmazonECRClient ecr, string ecruri, string projnm) {
    var dc = new DockerClientConfiguration(new Uri(OperatingSystem.IsWindows() ? "npipe://./pipe/docker_engine" : "unix:///var/run/docker.sock")).CreateClient();
    
    var dauth = new AuthConfig {
      ServerAddress = ecruri,
      Username = "AWS",
      Password = await GetEcrInputPassword(ecr)
    };
    var iuri = $"{ecruri}/{projnm}";

    await using var dfstream = CreateDockerContextTarStream(project.SolutionDirPath);
    await dc.Images.BuildImageFromDockerfileAsync(
      new ImageBuildParameters { Dockerfile = "Dockerfile", Tags = [iuri] }, 
      dfstream, [ dauth ], new Dictionary<string, string>(), new Progress<JSONMessage>(message => {
        if (!string.IsNullOrEmpty(message.Status)) Log.Information($"Build: {message.Status}");
        if (!string.IsNullOrEmpty(message.ErrorMessage)) throw new Exception($"Error: {message.Error}");
      }), CancellationToken.None);

    Log.Information("Image built successfully.");
    
    await dc.Images.PushImageAsync(ecruri,
      new ImagePushParameters(),
      dauth,
      new Progress<JSONMessage>(message => {
        if (!string.IsNullOrEmpty(message.Status)) Log.Information($"Push: {message.Status}");
        if (!string.IsNullOrEmpty(message.ErrorMessage)) throw new Exception($"Error: {message.Error}");
      }),
      CancellationToken.None);

    Log.Information("Image pushed successfully.");
  }
  
  private Stream CreateDockerContextTarStream(string contextPath)
{
    var tarStream = new MemoryStream();
    using (var tarArchive = new TarOutputStream(tarStream, Encoding.UTF8))
    {
        tarArchive.IsStreamOwner = false; // Important: Don't dispose the underlying stream

        // Add files from the context directory
        var filePaths = Directory.GetFiles(contextPath, "*.*", SearchOption.AllDirectories);
        foreach (var filePath in filePaths)
        {
            var relativePath = Path.GetRelativePath(contextPath, filePath)
                .Replace('\\', '/'); // Use forward slashes for Docker

            var entry = TarEntry.CreateEntryFromFile(filePath);
            entry.Name = relativePath;

            // Write the entry header
            tarArchive.PutNextEntry(entry);

            // Write the file contents
            using (var fileStream = File.OpenRead(filePath))
            {
                fileStream.CopyTo(tarArchive);
            }
            tarArchive.CloseEntry();
        }
    }

    tarStream.Position = 0; // Reset position to start
    return tarStream;
}


  private async Task<string> GetAccountId() {
    using var sts = new AmazonSecurityTokenServiceClient(credentials, region);
    return (await sts.GetCallerIdentityAsync(new GetCallerIdentityRequest())).Account;
  }

  private async Task<string> GetEcrInputPassword(AmazonECRClient ecr) {
    var res = await ecr.GetAuthorizationTokenAsync(new GetAuthorizationTokenRequest());
    if (res.AuthorizationData == null || res.AuthorizationData.Count == 0) throw new Exception("Failed to retrieve ECR authorization token.");
    return Encoding.UTF8.GetString(Convert.FromBase64String(res.AuthorizationData[0].AuthorizationToken)).Split(':')[1];
  }

  private static async Task<string> GetLatestImageDigest(AmazonECRClient ecr, string projnm) {
    var image = await ecr.DescribeImagesAsync(new DescribeImagesRequest { RepositoryName = projnm });
    var latest = image.ImageDetails
        .Where(img => img.ImageSizeInBytes > 1_048_576 && img.ArtifactMediaType is not null)
        .OrderByDescending(img => img.ImagePushedAt)
        .FirstOrDefault() ?? throw new Exception("No image found");
    return latest.ImageDigest;
  }

  private static async Task CheckAndCreateEcrRepository(AmazonECRClient ecr, string projnm) {
    try {
      await ecr.DescribeRepositoriesAsync(new DescribeRepositoriesRequest { RepositoryNames = [projnm] });
      Log.Information("Repository exists.");
    } catch (RepositoryNotFoundException) {
      Log.Information("Creating repository...");
      await ecr.CreateRepositoryAsync(new CreateRepositoryRequest { RepositoryName = projnm });
    }
  }

  private async Task<string> UpdateFunction(AmazonLambdaClient lambda, string imageUri) {
    await lambda.GetFunctionAsync(new GetFunctionRequest { FunctionName = project.AwsFunctionName });
    var res = await lambda.UpdateFunctionCodeAsync(new UpdateFunctionCodeRequest { FunctionName = project.AwsFunctionName, ImageUri = imageUri });
    return res.FunctionArn;
  }

  private async Task<string> CreateFunction(AmazonLambdaClient lambda, string imageuri, string accountid) {
    var res = await lambda.CreateFunctionAsync(new CreateFunctionRequest {
      FunctionName = project.AwsFunctionName,
      PackageType = PackageType.Image,
      Code = new FunctionCode { ImageUri = imageuri },
      Role = await GetOrCreateRole(project.RoleName, accountid),
      MemorySize = 256,
      Timeout = 30
    });
    return res.FunctionArn;
  }

  private async Task<bool> FunctionExists(IAmazonLambda client, string function) {
    try { return await client.GetFunctionAsync(new GetFunctionRequest { FunctionName = function }) is not null; }
    catch (ResourceNotFoundException) { return false; }
  }

  private async Task<string> GetOrCreateRole(string rolenm, string accountid) {
    using var aim = new AmazonIdentityManagementServiceClient(credentials);

    try { return (await aim.GetRoleAsync(new GetRoleRequest { RoleName = rolenm })).Role.Arn; }
    catch (NoSuchEntityException) { return await CreateRole(rolenm, accountid, aim); }
  }

  private async Task<string> CreateRole(string rolenm, string accountid, AmazonIdentityManagementServiceClient aim) {
    Log.Information($"IAM Role {rolenm} does not exist. Creating...");

    // Create the role with the trust policy
    var response = await aim.CreateRoleAsync(new CreateRoleRequest {
      RoleName = rolenm,
      AssumeRolePolicyDocument = templater.ParseFromPath("aws/lambda_policy.json", new { }),
      Description = "Role for AWS Lambda execution"
    });

    await aim.AttachRolePolicyAsync(new AttachRolePolicyRequest {
      RoleName = rolenm,
      PolicyArn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
    });

    await AddEcrAccessPolicyToRole(rolenm, accountid, aim);

    await AddEventBridgePolicy(rolenm, accountid, aim);

    await AddCloudWatchRolePolicy(rolenm, accountid, aim);

    await AddLambdaAccessPolicy(rolenm, accountid, aim);
    
    await AddSecretsManagerAccessPolicy(rolenm, accountid, aim);

    // Wait for role to propagate (IAM changes can take time to propagate)
    Log.Information("Waiting for IAM role to propagate...");
    await Task.Delay(10000); // 10 seconds delay

    Log.Information($"Created IAM Role: {response.Role.Arn}");
    return response.Role.Arn;
  }

  private async Task AddEcrAccessPolicyToRole(string rolenm, string accountid, AmazonIdentityManagementServiceClient aim) {
    await aim.PutRolePolicyAsync(new PutRolePolicyRequest {
      RoleName = rolenm,
      PolicyName = "lambda-ecr-access-" + rolenm,
      PolicyDocument = templater.ParseFromPath("aws/ecr_permission_policy.json", new {
        Region = region.SystemName,
        AccountId = accountid,
        ProjectName = project.ProjectName.ToLower()
      })
    });
  }

  private async Task AddSecretsManagerAccessPolicy(string rolenm, string accountid, AmazonIdentityManagementServiceClient aim) {
    await Task.WhenAll(project.Environments.Select(async e => {
      await aim.PutRolePolicyAsync(new PutRolePolicyRequest {
        RoleName = rolenm,
        PolicyName = "secretsmanager-access-" + rolenm,
        PolicyDocument = templater.ParseFromPath("aws/secretsmanager_permission_policy.json", new {
          Region = region.SystemName,
          AccountId = accountid,
          SecretsStoreId = settings.AwsSettings.GetSecretsStoreIdForEnvironment(e)
        })
      });
    }));
  }

  private async Task AddLambdaAccessPolicy(string rolenm, string accountid, AmazonIdentityManagementServiceClient aim) {
    await aim.PutRolePolicyAsync(new PutRolePolicyRequest {
      RoleName = rolenm,
      PolicyName = "lambda-access-" + rolenm,
      PolicyDocument = templater.ParseFromPath("aws/lambda_permission_policy.json", new {
        Region = region.SystemName,
        AccountId = accountid,
        FunctionName = project.AwsFunctionName
      })
    });
  }

  private async Task AddCloudWatchRolePolicy(string rolenm, string accountid, AmazonIdentityManagementServiceClient aim) {
    await aim.PutRolePolicyAsync(new PutRolePolicyRequest {
      RoleName = rolenm,
      PolicyName = "lambda-cloudwatch-access-" + rolenm,
      PolicyDocument = templater.ParseFromPath("aws/cloudwatch_permission_policy.json", new {
        Region = region.SystemName,
        AccountId = accountid,
        FunctionName = project.AwsFunctionName
      })
    });
  }

  private async Task AddEventBridgePolicy(string rolenm, string accountid, AmazonIdentityManagementServiceClient aim) {
    await aim.PutRolePolicyAsync(new PutRolePolicyRequest {
      RoleName = rolenm,
      PolicyName = "lambda-eventbridge-access-" + rolenm,
      PolicyDocument = templater.ParseFromPath("aws/eventbridge_permission_policy.json", new {
        Region = region.SystemName,
        AccountId = accountid,
        EventBusName = AwsEventBridgeChangesNotifier.EVENT_BUS_NAME
      })
    });
  }

  private async Task SetUpTimer(AmazonLambdaClient lambda, string funcarn) {
    using var evbridge = new AmazonEventBridgeClient(credentials, region);
    var rulenm = $"{project.AwsFunctionName}-TimerTrigger";
    var functype = IntegrationsAssemblyInspector.GetRequiredCentazioFunctions(project.Assembly, [project.AwsFunctionName]).Single();
    var func = IntegrationsAssemblyInspector.CreateFuncWithNullCtorArgs(functype);

    var rulearn = (await evbridge.PutRuleAsync(new PutRuleRequest {
      Name = rulenm,
      ScheduleExpression = $"cron({func.GetFunctionPollCronExpression(settings.Defaults).Value})",
      State = RuleState.ENABLED,
      Description = $"Trigger {project.AwsFunctionName} Lambda function every minute"
    })).RuleArn;
    Log.Information($"Created/Updated EventBridge rule: {rulearn}");

    // 3. Add permission for EventBridge to invoke the Lambda function (if not already present)
    try {
      await lambda.AddPermissionAsync(new AddPermissionRequest {
        FunctionName = project.AwsFunctionName,
        StatementId = $"{rulenm}-Permission", // Unique statement ID
        Action = "lambda:InvokeFunction",
        Principal = "events.amazonaws.com",
        SourceArn = rulearn
      });
      Log.Information("Added permission for EventBridge to invoke Lambda function");
    } catch (ResourceConflictException) {
      // Permission already exists - this is fine
      Log.Information("Permission already exists for EventBridge to invoke Lambda function");
    }

    // 4. Set the Lambda function as the target for the EventBridge rule
    var failed = (await evbridge.PutTargetsAsync(new PutTargetsRequest { Rule = rulenm, Targets = [new() { Id = "1", Arn = funcarn }] })).FailedEntries;
    if (failed.Any()) throw new Exception($"failed to set Lambda as target for EventBridge rule:\n\t" + string.Join("\n\t", failed.Select(f => f.ErrorMessage)));

    Log.Information("Successfully configured 1-minute trigger for Lambda function");
  }
}