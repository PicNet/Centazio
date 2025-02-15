using Centazio.Cli.Commands.Gen;
using Centazio.Cli.Infra;
using Centazio.Cli.Infra.Dotnet;
using Centazio.Core.Misc;
using Centazio.Core.Settings;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests;

public static class MiscHelpers {
  private static readonly CommandRunner cmd = new();
  private static readonly CentazioSettings settings = TestingFactories.Settings();
  
  public static FunctionProjectMeta EmptyFunctionProject(ECloudEnv cloud) => 
      new (ReflectionUtils.LoadAssembly("Centazio.TestFunctions"), cloud, TestingFactories.Settings().Defaults.GeneratedCodeFolder);

  public static class Az {
    public static List<string> ListFunctionApps() {
      var outstr = cmd.Az(settings.Parse(settings.Defaults.AzListFunctionAppsCmd)).Out;
      return String.IsNullOrWhiteSpace(outstr) ? [] : Json.Deserialize<List<NameObj>>(outstr).Select(r => r.Name).ToList();
    }
    
    public static List<string> ListFunctionsInApp(string appname) {
      var outstr = cmd.Az(settings.Parse(settings.Defaults.AzListFunctionsCmd, new { AppName = appname })).Out;
      return String.IsNullOrWhiteSpace(outstr) ? [] : Json.Deserialize<List<NameObj>>(outstr).Select(r => r.Name).ToList();
    }
    
    public static void DeleteFunctionApp(string appname) {
      cmd.Az(settings.Parse(settings.Defaults.AzDeleteFunctionAppCmd, new { AppName = appname }));
    }
    
    // ReSharper disable once ClassNeverInstantiated.Local
    private record NameObj(string Name);
  }
  
}