using Centazio.Cli.Infra.Misc;
using Centazio.Core.Misc;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests;

public static class MiscHelpers {
  private static readonly ICommandRunner cmd = new CommandRunner();
  // private static readonly ITemplater templater = new Templater(TestingFactories.Settings());
  
  public static async Task<AzFunctionProjectMeta> AzEmptyFunctionProject() => 
      new (ReflectionUtils.LoadAssembly("Centazio.TestFunctions"), await TestingFactories.Settings(), new Templater(await TestingFactories.Settings()));
  
  public static async Task<AwsFunctionProjectMeta> AwsEmptyFunctionProject(string function) => 
      new (ReflectionUtils.LoadAssembly("Centazio.TestFunctions"), await TestingFactories.Settings(), function);

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
  
}