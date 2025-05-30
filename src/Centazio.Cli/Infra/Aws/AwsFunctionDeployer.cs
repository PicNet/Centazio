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
using Amazon.SQS;
using Amazon.SQS.Model;
using Centazio.Core;
using Centazio.Core.Runner;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Hosts.Aws;
using Microsoft.Extensions.DependencyInjection;
using AddPermissionRequest = Amazon.Lambda.Model.AddPermissionRequest;
using Environment = System.Environment;
using ResourceNotFoundException = Amazon.Lambda.Model.ResourceNotFoundException;

namespace Centazio.Cli.Infra.Aws;

public interface IAwsFunctionDeployer {
  Task Deploy(AwsFunctionProjectMeta project);
}

public class AwsFunctionDeployer([FromKeyedServices(CentazioConstants.Hosts.Aws)] CentazioSettings settings, [FromKeyedServices(CentazioConstants.Hosts.Aws)] CentazioSecrets secrets, ITemplater templater) : IAwsFunctionDeployer {

  public async Task Deploy(AwsFunctionProjectMeta project) =>
      await new AwsFunctionDeployerImpl(settings, new(secrets.AWS_KEY, secrets.AWS_SECRET), project, templater).DeployImpl();
}

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
    BuildAndPushDockerImage(ecruri, projnm);

    if (!settings.AwsSettings.EventBridge) { await CreateSqsQueue(); }

    using var lambda = new AmazonLambdaClient(credentials, region);
    var funcarn = await UpdateOrCreateLambdaFunction(lambda, ecruri, projnm, ecr, accid);
    await SetUpTimer(lambda, funcarn);
    if (settings.AwsSettings.EventBridge) { await SetupEventBridge(lambda, funcarn); }
  }

  private async Task CreateSqsQueue() {
    var sqs = new AmazonSQSClient(credentials, region);
    await sqs.CreateQueueAsync(new CreateQueueRequest {
      QueueName = AwsSqsMessageBus.DEFAULT_QUEUE_NAME,
      Attributes = new Dictionary<string, string> {
        [QueueAttributeName.ReceiveMessageWaitTimeSeconds] = "20",
        [QueueAttributeName.MessageRetentionPeriod] = "60",
        [QueueAttributeName.VisibilityTimeout] = "30"
      }
    });
  }

  private async Task<string> UpdateOrCreateLambdaFunction(AmazonLambdaClient lambda, string ecruri, string projnm, AmazonECRClient ecr, string accid) {
    var imguri = $"{ecruri}/{projnm}@{await GetLatestImageDigest(ecr, projnm)}";
    return await FunctionExists(lambda, project.AwsFunctionName) ? await UpdateFunction(lambda, imguri) : await CreateFunction(lambda, imguri, accid);
  }

  private void BuildAndPushDockerImage(string ecruri, string projnm) {
    var dockercmds = settings.Defaults.ConsoleCommands.Docker;

    // todo CP: FIX the following docker command return an error even if the image is built successfully
    try { Run(dockercmds.Build, new { EcrUri = ecruri, ProjectName = projnm }, quiet: true); } 
    catch (Exception e) { Log.Warning(e, "Error running docker command"); }

    Run(dockercmds.LogIn, new { EcrUri = ecruri }, input: GetEcrInputPassword());
    Run(dockercmds.Push, new { EcrUri = ecruri, ProjectName = projnm });

    void Run(string command, object model, bool quiet = false, string? input = null) =>
        cmd.Docker(templater.ParseFromContent(command, model), project.ProjectDirPath, quiet: quiet, input: input);
  }

  private async Task<string> GetAccountId() {
    using var sts = new AmazonSecurityTokenServiceClient(credentials, region);
    return (await sts.GetCallerIdentityAsync(new GetCallerIdentityRequest())).Account;
  }

  private string GetEcrInputPassword() {
    Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", credentials.GetCredentials().AccessKey);
    Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", credentials.GetCredentials().SecretKey);

    var results = cmd.Aws(templater.ParseFromContent(settings.Defaults.ConsoleCommands.AwsCmds.GetEcrPass, new { Region = region.SystemName }), project.ProjectDirPath);
    if (!results.Success) throw new Exception(results.Err);

    return results.Out;
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
    if (!settings.AwsSettings.EventBridge) {
      await lambda.CreateEventSourceMappingAsync(new CreateEventSourceMappingRequest {
        FunctionName = project.AwsFunctionName,
        EventSourceArn = $"arn:aws:sqs:{region.SystemName}:{accountId}:{AwsSqsMessageBus.DEFAULT_QUEUE_NAME}",
        BatchSize = 10,
        Enabled = true
      });
    }

    return res.FunctionArn;
  }

  private async Task<bool> FunctionExists(IAmazonLambda client, string function) {
    try { return await client.GetFunctionAsync(new GetFunctionRequest { FunctionName = function }) is not null; }
    catch (ResourceNotFoundException) { return false; }
  }

  private async Task<string> GetOrCreateRole(string rolenm, string accountId) {
    using var aim = new AmazonIdentityManagementServiceClient(credentials);

    try { return (await aim.GetRoleAsync(new GetRoleRequest { RoleName = rolenm })).Role.Arn; }
    catch (NoSuchEntityException) {
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

      await aim.PutRolePolicyAsync(new PutRolePolicyRequest {
        RoleName = rolenm,
        PolicyName = "LambdaECRAccess" + rolenm,
        PolicyDocument = templater.ParseFromPath("aws/ecr_policy.json", new {
          Region = region.SystemName,
          AccountId = accountId,
          ProjectName = project.ProjectName.ToLower()
        })
      });

      if (!settings.AwsSettings.EventBridge) {
        await aim.PutRolePolicyAsync(new PutRolePolicyRequest {
          RoleName = rolenm,
          PolicyName = "LambdaSQSAccess" + rolenm,
          PolicyDocument = templater.ParseFromPath("aws/sqs_policy.json",
              new {
                Region = region.SystemName,
                AccountId = accountId,
                QueueName = AwsSqsMessageBus.DEFAULT_QUEUE_NAME
              })
        });
      }
      else {
        // TODO policy for event bridge ??
      }

      await aim.PutRolePolicyAsync(new PutRolePolicyRequest {
        RoleName = rolenm,
        PolicyName = "LambdaCloudWatchAccess" + rolenm,
        PolicyDocument = templater.ParseFromPath("aws/cloudwatch_policy.json",
            new {
              Region = region.SystemName,
              AccountId = accountId,
              FunctionName = project.AwsFunctionName.ToLower()
            })
      });

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
    } catch (ResourceConflictException) {
      // Permission already exists - this is fine
      Log.Information("Permission already exists for EventBridge to invoke Lambda function");
    }

    // 4. Set the Lambda function as the target for the EventBridge rule
    var failed = (await evbridge.PutTargetsAsync(new PutTargetsRequest { Rule = rulenm, Targets = [new() { Id = "1", Arn = funcarn }] })).FailedEntries;
    if (failed.Any()) throw new Exception($"failed to set Lambda as target for EventBridge rule:\n\t" + string.Join("\n\t", failed.Select(f => f.ErrorMessage)));

    Log.Information("Successfully configured 1-minute trigger for Lambda function");
  }

  private async Task SetupEventBridge(AmazonLambdaClient lambda, string funcarn) {
    var octs = project.Config()?.Operations.SelectMany(op => op.Triggers).ToList();
    if (octs != null) { await Task.WhenAll(octs.Select(trigger => {
      var evbridge = new AmazonEventBridgeClient(credentials, region);
      return CreateEventBridgeRule(lambda, evbridge, funcarn, trigger);
    })); }
  }

  private async Task CreateEventBridgeRule(AmazonLambdaClient lambda, AmazonEventBridgeClient evbridge, string funcarn, ObjectChangeTrigger trigger) {
    var rulenm = $"ebr-{project.AwsFunctionName}-{trigger.System}-{trigger.Stage}-{trigger.Object}";
    var putRuleRequest = new PutRuleRequest {
      Name = rulenm,
      EventPattern = templater.ParseFromPath("aws/eventbridge_rule.json",
          new {
            System = trigger.System.Value,
            Stage = trigger.Stage.Value,
            Object = trigger.Object.Value
          }),
      State = RuleState.ENABLED,
      Description = $"Trigger Lambda on System [{trigger.System}] Stage [{trigger.Stage.Value}] Object [{trigger.Object.Value}]",
      EventBusName = "default" // Or your custom event bus
    };

    var putRuleResponse = await evbridge.PutRuleAsync(putRuleRequest);
    var rulearn = putRuleResponse.RuleArn;

    var putTargetsRequest = new PutTargetsRequest {
      Rule = rulenm,
      EventBusName = "default",
      Targets = new List<Target> {
        new() {
          Id = $"ebr-tgt-{rulenm}",
          Arn = funcarn
        }
      }
    };

    await evbridge.PutTargetsAsync(putTargetsRequest);

    try {
      await lambda.AddPermissionAsync(new AddPermissionRequest {
        FunctionName = project.AwsFunctionName,
        StatementId = $"{rulenm}-permission",
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
  }

}