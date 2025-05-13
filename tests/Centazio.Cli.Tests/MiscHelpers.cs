using Amazon;
using Amazon.Lambda;
using Amazon.Runtime;
using Centazio.Cli.Infra.Misc;
using Centazio.Core.Misc;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests;

public static class MiscHelpers {
  private static readonly ICommandRunner cmd = new CommandRunner();
  private static readonly CentazioSettings settings = TestingFactories.Settings();
  private static readonly CentazioSecrets secrets = TestingFactories.Secrets();
  private static readonly ITemplater templater = new Templater(TestingFactories.Settings());
  
  public static AzFunctionProjectMeta AzEmptyFunctionProject() => 
      new (ReflectionUtils.LoadAssembly("Centazio.TestFunctions"), TestingFactories.Settings(), templater);
  
  public static AwsFunctionProjectMeta AwsEmptyFunctionProject(string function) => 
      new (ReflectionUtils.LoadAssembly("Centazio.TestFunctions"), TestingFactories.Settings(), function);

  public static class Az {
    public static List<string> ListFunctionApps() {
      var outstr = cmd.Az(templater.ParseFromContent(settings.Defaults.ConsoleCommands.Az.ListFunctionApps)).Out;
      return String.IsNullOrWhiteSpace(outstr) ? [] : Json.Deserialize<List<NameObj>>(outstr).Select(r => r.Name).ToList();
    }
    
    public static List<string> ListFunctionsInApp(string appname) {
      var outstr = cmd.Az(templater.ParseFromContent(settings.Defaults.ConsoleCommands.Az.ListFunctions, new { AppName = appname })).Out;
      return String.IsNullOrWhiteSpace(outstr) ? [] : Json.Deserialize<List<NameObj>>(outstr).Select(r => r.Name).ToList();
    }
    
    public static void DeleteFunctionApp(string appname) {
      cmd.Az(templater.ParseFromContent(settings.Defaults.ConsoleCommands.Az.DeleteFunctionApp, new { AppName = appname }));
    }
    
    // ReSharper disable once ClassNeverInstantiated.Local
    private record NameObj(string Name);
  }
  
  public static class Aws {
    public static async Task<List<string>> ListFunctionApps() {
      using var lambda = GetAmazonLambdaClient();
      var functions = await lambda.ListFunctionsAsync();
      return functions.Functions.Select(r => r.FunctionName).ToList();
    }

    public static async Task<List<string>> ListFunctionsInApp(string appname) {
      return (await ListFunctionApps()).Where(r => r.StartsWith(appname)).ToList();
    }
    
    public static async Task DeleteFunctionApp(string appname) {
      using var lambda = GetAmazonLambdaClient();
      await lambda.DeleteFunctionAsync(appname);
    }
    
    private static AmazonLambdaClient GetAmazonLambdaClient()
    {
      AmazonLambdaClient? lambda = null;
      try
      {
        var region = RegionEndpoint.GetBySystemName(settings.AwsSettings.Region);
        lambda = new AmazonLambdaClient(new BasicAWSCredentials(secrets.AWS_KEY, secrets.AWS_SECRET), region);
        return lambda;
      }
      catch
      {
        lambda?.Dispose();
        throw;
      }
    }
    
    // ReSharper disable once ClassNeverInstantiated.Local
    private record NameObj(string Name);
  }
}