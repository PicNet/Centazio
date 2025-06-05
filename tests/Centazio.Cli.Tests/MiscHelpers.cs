using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.Runtime;
using Centazio.Cli.Infra.Misc;
using Centazio.Core.Misc;
using Centazio.Providers.Aws;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests;

public static class MiscHelpers {
  private static readonly ICommandRunner cmd = new CommandRunner();
  public static async Task<AzFunctionProjectMeta> AzEmptyFunctionProject() => 
      new (ReflectionUtils.LoadAssembly("Centazio.TestFunctions"), await TestingFactories.Settings(), new Templater(await TestingFactories.Settings()));
  
  public static async Task<AwsFunctionProjectMeta> AwsEmptyFunctionProject(string function) => 
      new (ReflectionUtils.LoadAssembly("Centazio.TestFunctions"), await TestingFactories.Settings(), function, new List<string> { "testing" });

  public static class Az {
    public static async Task<List<string>> ListFunctionApps() {
      var settings = await TestingFactories.Settings();
      var templater = new Templater(settings);
      var outstr = cmd.Az(templater.ParseFromContent(settings.Defaults.ConsoleCommands.Az.ListFunctionApps)).Out;
      return String.IsNullOrWhiteSpace(outstr) ? [] : Json.Deserialize<List<NameObj>>(outstr).Select(r => r.Name).ToList();
    }
    
    public static async Task<List<string>> ListFunctionsInApp(string appname) {
      var settings = await TestingFactories.Settings();
      var templater = new Templater(settings);
      var outstr = cmd.Az(templater.ParseFromContent(settings.Defaults.ConsoleCommands.Az.ListFunctions, new { AppName = appname })).Out;
      return String.IsNullOrWhiteSpace(outstr) ? [] : Json.Deserialize<List<NameObj>>(outstr).Select(r => r.Name).ToList();
    }
    
    public static async Task DeleteFunctionApp(string appname) {
      var settings = await TestingFactories.Settings();
      var templater = new Templater(settings);
      cmd.Az(templater.ParseFromContent(settings.Defaults.ConsoleCommands.Az.DeleteFunctionApp, new { AppName = appname }));
    }
    
    // ReSharper disable once ClassNeverInstantiated.Local
    private record NameObj(string Name);
  }
  
  public static class Aws {
    public static async Task<List<string>> ListFunctionApps() {
      using var lambda = await GetAmazonLambdaClient();
      var functions = await lambda.ListFunctionsAsync();
      return functions.Functions.Select(r => r.FunctionName).ToList();
    }

    public static async Task<List<string>> ListFunctionsInApp(string appname) {
      return (await ListFunctionApps()).Where(r => r.StartsWith(appname)).ToList();
    }
    
    public static async Task DeleteFunctionApp(string appname) {
      using var lambda = await GetAmazonLambdaClient();
      
      var esm = await lambda.ListEventSourceMappingsAsync(new ListEventSourceMappingsRequest() { FunctionName = appname });
      if (esm.EventSourceMappings.Any()) {
        esm.EventSourceMappings.ForEach(async void (e) => { await lambda.DeleteEventSourceMappingAsync(new DeleteEventSourceMappingRequest { UUID = e.UUID }); });
      }
      await lambda.DeleteFunctionAsync(appname);
    }

    private static async Task<AmazonLambdaClient> GetAmazonLambdaClient() {
      var settings = await TestingFactories.Settings();
      var secrets = await TestingFactories.Secrets();
      AmazonLambdaClient? lambda = null;
      try {
        var region = settings.AwsSettings.GetRegionEndpoint();
        lambda = new AmazonLambdaClient(new BasicAWSCredentials(secrets.AWS_KEY, secrets.AWS_SECRET), region);
        return lambda;
      } catch {
        lambda?.Dispose();
        throw;
      }
    }

  }

}