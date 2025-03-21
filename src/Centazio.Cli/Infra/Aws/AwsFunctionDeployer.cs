using Amazon;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.Runtime;
using Centazio.Core.Runner;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using ResourceNotFoundException = Amazon.Lambda.Model.ResourceNotFoundException;

namespace Centazio.Cli.Infra.Aws;

public interface IAwsFunctionDeployer {
  Task Deploy(AwsFunctionProjectMeta project);
}

public class AwsFunctionDeployer(CentazioSettings settings, CentazioSecrets secrets) : IAwsFunctionDeployer {

  public async Task Deploy(AwsFunctionProjectMeta project) => 
      await new AwsFunctionDeployerImpl(settings, new(secrets.AWS_KEY, secrets.AWS_SECRET), project).DeployImpl();

  class AwsFunctionDeployerImpl(CentazioSettings settings, BasicAWSCredentials credentials, AwsFunctionProjectMeta project) {

    private readonly RegionEndpoint region = RegionEndpoint.GetBySystemName(settings.AwsSettings.Region);
    
    public async Task DeployImpl() {
      if (!Directory.Exists(project.SolutionDirPath)) throw new Exception($"project [{project.ProjectName}] could not be found in the [{settings.Defaults.GeneratedCodeFolder}] folder");
      if (!File.Exists(project.SlnFilePath)) throw new Exception($"project [{project.ProjectName}] does not appear to be a valid as no sln file was found");

      using var lambda = new AmazonLambdaClient(credentials, region);
      var zipbytes = await Zip.ZipDir(project.PublishPath, [".exe", ".dll", ".json", ".env", "*.metadata", ".pdb"], []);
      var funcarn = await GetOrCreateLambdaFunction();
      await SetUpTimer(lambda, funcarn);
      await SetUpLogging();
      
      Log.Information("Deployment completed successfully!");

      async Task<string> GetOrCreateLambdaFunction() {
        if (await FunctionExists(lambda, project.AwsFunctionName)) {
          Log.Information($"Function {project.AwsFunctionName} exists. Updating...");
          return await UpdateFunctionCodeAsync(lambda, zipbytes);
        }

        Log.Information($"Function {project.AwsFunctionName} does not exist. Creating...");
        return await CreateFunctionAsync(lambda, zipbytes);
      }
    }

    private async Task<bool> FunctionExists(IAmazonLambda client, string function) {
      try { return await client.GetFunctionAsync(new GetFunctionRequest { FunctionName = function }) is not null; }
      catch (ResourceNotFoundException) { return false; }
    }

    private async Task<string> CreateFunctionAsync(IAmazonLambda client, byte[] zipbytes) {
      var req = new CreateFunctionRequest {
        FunctionName = project.AwsFunctionName,
        Runtime = "dotnet8",
        Role = await GetOrCreateRole(project.RoleName),
        Handler = project.HandlerName,
        Code = new FunctionCode { ZipFile = new MemoryStream(zipbytes) },
        Timeout = 30, // 30 seconds timeout
        MemorySize = 256 // 256 MB memory allocation
      };

      var response = await client.CreateFunctionAsync(req);
      Log.Information($"Created function: {response.FunctionArn}");
      return response.FunctionArn;
    }

    private async Task<string> GetOrCreateRole(string rolenm) {
      using var aim = new AmazonIdentityManagementServiceClient(credentials, region);

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

        // Wait for role to propagate (IAM changes can take time to propagate)
        Log.Information("Waiting for IAM role to propagate...");
        await Task.Delay(10000); // 10 seconds delay

        Log.Information($"Created IAM Role: {response.Role.Arn}");
        return response.Role.Arn;
      }
    }

    private async Task<string> UpdateFunctionCodeAsync(IAmazonLambda client, byte[] zipbytes) {
      var response = await client.UpdateFunctionCodeAsync(new UpdateFunctionCodeRequest { FunctionName = project.AwsFunctionName, ZipFile = new MemoryStream(zipbytes) });
      Log.Information($"Updated function: {response.FunctionArn}");
      return response.FunctionArn;
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
      var failed = (await evbridge.PutTargetsAsync(new PutTargetsRequest { Rule = rulenm, Targets = [ new() { Id = "1", Arn = funcarn }] })).FailedEntries;
      if (failed.Any()) throw new Exception($"failed to set Lambda as target for EventBridge rule:\n\t" + String.Join("\n\t", failed.Select(f => f.ErrorMessage)));
      Log.Information("Successfully configured 1-minute trigger for Lambda function");
    }

    private async Task SetUpLogging() {
      using var cloudwatch = new AmazonCloudWatchLogsClient(credentials, region);
      var loggrpnm = $"/aws/lambda/{project.AwsFunctionName}";

      var exists = (await cloudwatch.DescribeLogGroupsAsync(new DescribeLogGroupsRequest { LogGroupNamePrefix = loggrpnm })).LogGroups.Exists(lg => lg.LogGroupName == loggrpnm);
      if (exists) { Log.Information($"CloudWatch Logs group already exists: {loggrpnm}"); return; }

      await cloudwatch.CreateLogGroupAsync(new CreateLogGroupRequest { LogGroupName = loggrpnm });
      await cloudwatch.PutRetentionPolicyAsync(new PutRetentionPolicyRequest { LogGroupName = loggrpnm, RetentionInDays = 14 });

      Log.Information($"Created CloudWatch Logs group: {loggrpnm} with 30-day retention");
    }
  }
}