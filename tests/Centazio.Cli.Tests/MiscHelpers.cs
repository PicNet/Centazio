using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.Runtime;
using Centazio.Cli.Infra.Misc;
using Centazio.Core.Misc;
using Centazio.Providers.Aws;

namespace Centazio.Cli.Tests;

public static class MiscHelpers {
  private static readonly ICommandRunner cmd = new CommandRunner();
  public static async Task<AzFunctionProjectMeta> AzEmptyFunctionProject() => 
      new (ReflectionUtils.LoadAssembly("Centazio.TestFunctions"), await F.Settings(), new Templater(await F.Settings()));
  
  public static async Task<AwsFunctionProjectMeta> AwsEmptyFunctionProject(string function) => 
      new (ReflectionUtils.LoadAssembly("Centazio.TestFunctions"), await F.Settings(), function, new List<string> { "testing" });

  public static class Az {
    public static async Task<List<string>> ListFunctionApps() {
      var settings = await F.Settings();
      var templater = new Templater(settings);
      var result = await cmd.Az(templater.ParseFromContent(settings.Defaults.ConsoleCommands.Az.ListFunctionApps));
      return String.IsNullOrWhiteSpace(result.Out) ? [] : Json.Deserialize<List<NameObj>>(result.Out).Select(r => r.Name).ToList();
    }
    
    public static async Task<List<string>> ListFunctionsInApp(string appname) {
      var settings = await F.Settings();
      var templater = new Templater(settings);
      var result = await cmd.Az(templater.ParseFromContent(settings.Defaults.ConsoleCommands.Az.ListFunctions, new { AppName = appname })); 
      return String.IsNullOrWhiteSpace(result.Out) ? [] : Json.Deserialize<List<NameObj>>(result.Out).Select(r => r.Name).ToList();
    }
    
    public static async Task DeleteFunctionApp(string appname) {
      var settings = await F.Settings();
      var templater = new Templater(settings);
      await cmd.Az(templater.ParseFromContent(settings.Defaults.ConsoleCommands.Az.DeleteFunctionApp, new { AppName = appname }));
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
        // todo CP: do not use async void with ForEach use Synchronous or Task.WhenAll
        esm.EventSourceMappings.ForEach(async void (e) => { await lambda.DeleteEventSourceMappingAsync(new DeleteEventSourceMappingRequest { UUID = e.UUID }); });
      }
      await lambda.DeleteFunctionAsync(appname);
    }

    private static async Task<AmazonLambdaClient> GetAmazonLambdaClient() {
      var settings = await F.Settings();
      var secrets = await F.Secrets();
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