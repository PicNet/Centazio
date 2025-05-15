using Amazon;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
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
using Environment = System.Environment;
using ResourceNotFoundException = Amazon.Lambda.Model.ResourceNotFoundException;

namespace Centazio.Cli.Infra.Aws;

public interface IAwsFunctionDeployer {

  Task Deploy(AwsFunctionProjectMeta project);

}

public class AwsFunctionDeployer(CentazioSettings settings, CentazioSecrets secrets) : IAwsFunctionDeployer {

  public async Task Deploy(AwsFunctionProjectMeta project) =>
      await new AwsFunctionDeployerImpl(settings, new(secrets.AWS_KEY, secrets.AWS_SECRET), project).DeployImpl();

  private class AwsFunctionDeployerImpl(CentazioSettings settings, BasicAWSCredentials credentials, AwsFunctionProjectMeta project) {

    private readonly ICommandRunner cmd = new CommandRunner();
    private readonly RegionEndpoint region = RegionEndpoint.GetBySystemName(settings.AwsSettings.Region);

    public async Task DeployImpl() {
      if (!Directory.Exists(project.SolutionDirPath)) throw new Exception($"project [{project.ProjectName}] could not be found in the [{settings.Defaults.GeneratedCodeFolder}] folder");
      if (!File.Exists(project.SlnFilePath)) throw new Exception($"project [{project.ProjectName}] does not appear to be a valid as no sln file was found");

      var projectName = project.ProjectName.ToLower();

      await SetUpLogging();

      using var ecrClient = new AmazonECRClient(credentials, region);

      var accountId = await GetAccountId();

      await CheckAndCreateEcrRepository(ecrClient, projectName);

      var ecrUri = $"{accountId}.dkr.ecr.{region.SystemName}.amazonaws.com";

      BuildAndPushDockerImage(ecrUri, projectName);

      using var lambda = new AmazonLambdaClient(credentials, region);
      var funcArn = await UpdateOrCreateLambdaFunction(lambda, ecrUri, projectName, ecrClient, accountId);
      await SetUpTimer(lambda, funcArn);
    }

    private async Task<string> UpdateOrCreateLambdaFunction(AmazonLambdaClient lambda, string ecrUri, string projectName, AmazonECRClient ecrClient, string accountId) {
      var imageUri = $"{ecrUri}/{projectName}@{await GetLatestImageDigest(ecrClient, projectName)}";
      return await FunctionExists(lambda, project.AwsFunctionName) ? await UpdateFunction(lambda, imageUri) : await CreateFunction(lambda, imageUri, accountId);
    }

    private void BuildAndPushDockerImage(string ecrUri, string projectName) {
      // FIX the following docker command return an error even if the image is built successfully
      try {
        cmd.Docker($"build -t {ecrUri}/{projectName} .", project.ProjectDirPath, true);
      } catch (Exception e) {
        Log.Warning(e, "Error running docker command");
      }

      cmd.Docker(@$"login --username AWS --password-stdin {ecrUri}", project.ProjectDirPath, input: GetEcrInputPassword());
      cmd.Docker(@$"push {ecrUri}/{projectName}", project.ProjectDirPath);
    }

    private async Task<string> GetAccountId() {
      using var stsClient = new AmazonSecurityTokenServiceClient(credentials, region);
      var identityResponse = await stsClient.GetCallerIdentityAsync(new GetCallerIdentityRequest());
      var accountId = identityResponse.Account;
      return accountId;
    }

    private string GetEcrInputPassword() {
      Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", credentials.GetCredentials().AccessKey);
      Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", credentials.GetCredentials().SecretKey);

      var results1 = cmd.Aws(@$"ecr get-login-password --region {region.SystemName}", project.ProjectDirPath);
      if (!results1.Success) throw new Exception(results1.Err);

      var inputPassword = results1.Out;
      return inputPassword;
    }

    private static async Task<string> GetLatestImageDigest(AmazonECRClient ecrClient, string projectName) {
      var imageDetails = await ecrClient.DescribeImagesAsync(new DescribeImagesRequest {
        RepositoryName = projectName
      });

      var latestImage = imageDetails.ImageDetails
          .Where(img => img.ImageSizeInBytes > 1_048_576 && img.ArtifactMediaType is not null)
          .OrderByDescending(img => img.ImagePushedAt)
          .FirstOrDefault();

      if (latestImage is null) throw new Exception("No image found");

      var latestImageDigest = latestImage.ImageDigest;
      return latestImageDigest;
    }

    private static async Task CheckAndCreateEcrRepository(AmazonECRClient ecrClient, string projectName) {
      try {
        await ecrClient.DescribeRepositoriesAsync(new DescribeRepositoriesRequest {
          RepositoryNames = [projectName]
        });
        Log.Information("Repository exists.");
      }
      catch (RepositoryNotFoundException) {
        Log.Information("Creating repository...");
        await ecrClient.CreateRepositoryAsync(new CreateRepositoryRequest {
          RepositoryName = projectName
        });
      }
    }

    private async Task<string> UpdateFunction(AmazonLambdaClient lambda, string imageUri) {
      await lambda.GetFunctionAsync(new GetFunctionRequest { FunctionName = project.AwsFunctionName });
      var res = await lambda.UpdateFunctionCodeAsync(new UpdateFunctionCodeRequest { FunctionName = project.AwsFunctionName, ImageUri = imageUri });
      return res.FunctionArn;
    }

    private async Task<string> CreateFunction(AmazonLambdaClient lambda, string imageUri, string accountId) {
      var res = await lambda.CreateFunctionAsync(new CreateFunctionRequest {
        FunctionName = project.AwsFunctionName,
        PackageType = PackageType.Image,
        Code = new FunctionCode {
          ImageUri = imageUri
        },
        Role = await GetOrCreateRole(project.RoleName, accountId),
        MemorySize = 256,
        Timeout = 30
      });
      return res.FunctionArn;
    }

    private async Task<bool> FunctionExists(IAmazonLambda client, string function) {
      try { return await client.GetFunctionAsync(new GetFunctionRequest { FunctionName = function }) is not null; }
      catch (ResourceNotFoundException) { return false; }
    }

    private async Task<string> GetOrCreateRole(string rolenm, string accountId) {
      using var aim = new AmazonIdentityManagementServiceClient(credentials);

      try {
        return (await aim.GetRoleAsync(new GetRoleRequest { RoleName = rolenm })).Role.Arn;
      }
      catch (NoSuchEntityException) {
        Log.Information($"IAM Role {rolenm} does not exist. Creating...");
        var policy = @"{ ""Version"": ""2012-10-17"", ""Statement"": [ {
    ""Effect"": ""Allow"",
    ""Principal"": { ""Service"": ""lambda.amazonaws.com"" },
    ""Action"": ""sts:AssumeRole"" } ] }";

        // Create the role with the trust policy
        var createreq = new CreateRoleRequest {
          RoleName = rolenm,
          AssumeRolePolicyDocument = policy,
          Description = "Role for AWS Lambda execution"
        };

        var response = await aim.CreateRoleAsync(createreq);

        var permission = new AttachRolePolicyRequest {
          RoleName = rolenm,
          PolicyArn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
        };

        await aim.AttachRolePolicyAsync(permission);

        var policyName = "LambdaECRAccess" + rolenm;

        var ecrPolicyJson = $@"{{
  ""Version"": ""2012-10-17"",
  ""Statement"": [
    {{
      ""Effect"": ""Allow"",
      ""Action"": [
        ""ecr:GetDownloadUrlForLayer"",
        ""ecr:BatchGetImage"",
        ""ecr:BatchCheckLayerAvailability""
      ],
      ""Resource"": ""arn:aws:ecr:{region.SystemName}:{accountId}:repository/{project.ProjectName.ToLower()}""
    }}
  ]
}}";

        var putPolicyRequest = new PutRolePolicyRequest {
          RoleName = rolenm,
          PolicyName = policyName,
          PolicyDocument = ecrPolicyJson
        };

        await aim.PutRolePolicyAsync(putPolicyRequest);

        // Wait for role to propagate (IAM changes can take time to propagate)
        Log.Information("Waiting for IAM role to propagate...");
        await Task.Delay(10000); // 10 seconds delay

        Log.Information($"Created IAM Role: {response.Role.Arn}");
        return response.Role.Arn;
      }
    }

    private async Task SetUpTimer(AmazonLambdaClient lambda, string funcarn) {
      using var evbridge = new AmazonEventBridgeClient(credentials, region);
      var rulenm = $"{project.AwsFunctionName}-TimerTrigger";
      var functype = IntegrationsAssemblyInspector.GetCentazioFunctions(project.Assembly, [project.AwsFunctionName]).Single();
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
      }
      catch (ResourceConflictException) {
        // Permission already exists - this is fine
        Log.Information("Permission already exists for EventBridge to invoke Lambda function");
      }

      // 4. Set the Lambda function as the target for the EventBridge rule
      var failed = (await evbridge.PutTargetsAsync(new PutTargetsRequest { Rule = rulenm, Targets = [new() { Id = "1", Arn = funcarn }] })).FailedEntries;
      if (failed.Any()) throw new Exception($"failed to set Lambda as target for EventBridge rule:\n\t" + string.Join("\n\t", failed.Select(f => f.ErrorMessage)));

      Log.Information("Successfully configured 1-minute trigger for Lambda function");
    }

    private async Task SetUpLogging() {
      using var cloudwatch = new AmazonCloudWatchLogsClient(credentials, region);
      var loggrpnm = $"/aws/lambda/{project.AwsFunctionName}";

      var exists = (await cloudwatch.DescribeLogGroupsAsync(new DescribeLogGroupsRequest { LogGroupNamePrefix = loggrpnm })).LogGroups.Exists(lg => lg.LogGroupName == loggrpnm);
      if (exists) {
        Log.Information($"CloudWatch Logs group already exists: {loggrpnm}");
        return;
      }

      await cloudwatch.CreateLogGroupAsync(new CreateLogGroupRequest { LogGroupName = loggrpnm });
      await cloudwatch.PutRetentionPolicyAsync(new PutRetentionPolicyRequest { LogGroupName = loggrpnm, RetentionInDays = 14 });

      Log.Information($"Created CloudWatch Logs group: {loggrpnm} with 30-day retention");
    }

  }

}