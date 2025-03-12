using Amazon;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.Runtime;
using Centazio.Cli.Infra.Misc;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;

namespace Centazio.Cli.Infra.Aws;

public interface IAwsFunctionDeployer {
  Task Deploy(FunctionProjectMeta project);
}

public class AwsFunctionDeployer(CentazioSettings settings, CentazioSecrets secrets) : IAwsFunctionDeployer {

  public async Task Deploy(FunctionProjectMeta project) {
    if (!Directory.Exists(project.SolutionDirPath)) throw new Exception($"project [{project.ProjectName}] could not be found in the [{settings.Defaults.GeneratedCodeFolder}] folder");
    if (!File.Exists(project.SlnFilePath)) throw new Exception($"project [{project.ProjectName}] does not appear to be a valid as no sln file was found");

    var creds = new BasicAWSCredentials(secrets.AWS_KEY, secrets.AWS_SECRET);
    using var client = new AmazonLambdaClient(creds, RegionEndpoint.GetBySystemName(settings.AwsSettings.Region));

    var zipbytes = await Zip.ZipDir(project.PublishPath, [".exe", ".dll", ".json", ".env", "*.metadata", ".pdb"], ["<todo: subdirectories>"]);
    if (await FunctionExists(client, project.AwsFunctionName)) {
      Log.Information($"Function {project.AwsFunctionName} exists. Updating...");
      await UpdateFunctionCodeAsync(client, project.AwsFunctionName, zipbytes);
    }
    else {
      Log.Information($"Function {project.AwsFunctionName} does not exist. Creating...");
      await CreateFunctionAsync(client, project, zipbytes);
    }

    Log.Information("Deployment completed successfully!");
  }

  private async Task<bool> FunctionExists(IAmazonLambda client, string function) {
    try { return await client.GetFunctionAsync(new GetFunctionRequest { FunctionName = function }) is not null; }
    catch (ResourceNotFoundException) { return false; }
  }

  private async Task CreateFunctionAsync(IAmazonLambda client, FunctionProjectMeta project, byte[] zipbytes) {
    var req = new CreateFunctionRequest {
      FunctionName = project.AwsFunctionName,

      // todo: `dotnet8` should be in 'defaults' settings
      Runtime = "dotnet8",
      Role = await GetOrCreateRole(project.AwsRoleName),
      Handler = project.AwsHandlerName,
      Code = new FunctionCode { ZipFile = new MemoryStream(zipbytes) },
      Timeout = 30, // 30 seconds timeout
      MemorySize = 256 // 256 MB memory allocation
    };

    var response = await client.CreateFunctionAsync(req);
    Log.Information($"Created function: {response.FunctionArn}");
  }

  private async Task<string> GetOrCreateRole(string rolenm) {
    var creds = new BasicAWSCredentials(secrets.AWS_KEY, secrets.AWS_SECRET);
    using var client = new AmazonIdentityManagementServiceClient(creds, RegionEndpoint.GetBySystemName(settings.AwsSettings.Region));

    try {
      return (await client.GetRoleAsync(new GetRoleRequest { RoleName = rolenm })).Role.Arn;
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

      var response = await client.CreateRoleAsync(createreq);

      var permission = new AttachRolePolicyRequest {
        RoleName = rolenm,
        PolicyArn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
      };

      await client.AttachRolePolicyAsync(permission);

      // Wait for role to propagate (IAM changes can take time to propagate)
      Log.Information("Waiting for IAM role to propagate...");
      await Task.Delay(10000); // 10 seconds delay

      Log.Information($"Created IAM Role: {response.Role.Arn}");
      return response.Role.Arn;
    }
  }

  private async Task UpdateFunctionCodeAsync(IAmazonLambda client, string function, byte[] zipbytes) {
    var response = await client.UpdateFunctionCodeAsync(new UpdateFunctionCodeRequest { FunctionName = function, ZipFile = new MemoryStream(zipbytes) });
    Log.Information($"Updated function: {response.FunctionArn}");
  }

}