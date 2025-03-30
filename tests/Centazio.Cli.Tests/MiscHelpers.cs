using Centazio.Cli.Infra.Misc;
using Centazio.Core.Misc;
using Centazio.Core.Settings;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests;

public static class MiscHelpers {
  private static readonly ICommandRunner cmd = new CommandRunner();
  private static readonly CentazioSettings settings = TestingFactories.Settings();
  private static readonly ITemplater templater = new Templater(TestingFactories.Settings(), TestingFactories.Secrets());
  
  public static AzureFunctionProjectMeta AzureEmptyFunctionProject() => 
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
  
}