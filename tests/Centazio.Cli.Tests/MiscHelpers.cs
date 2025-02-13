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
      new (ReflectionUtils.LoadAssembly("Centazio.TestFunctions"), cloud, TestingFactories.Settings().GeneratedCodeFolder);

  public static class Az {
    private static AzureSettings azcfg = settings.AzureSettings;
    private static string rg = azcfg.ResourceGroup;
    
    public static List<string> ListFunctionApps() {
      var outstr = cmd.Az($"functionapp list -g {rg} --query \"[].{{Name:name}}\"").Out;
      return Json.Deserialize<List<NameObj>>(outstr).Select(r => r.Name).ToList();
    }
    
    public static List<string> ListFunctionsInApp(string appname) {
      var outstr = cmd.Az($"functionapp function list -g {rg} -n {appname} --query \"[].{{Name:name}}\"").Out;
      return Json.Deserialize<List<NameObj>>(outstr).Select(r => r.Name).ToList();
    }
    
    public static void DeleteFunctionApp(string appname) {
      cmd.Az($"functionapp delete -g {rg} -n {appname}");
    }
    
    // ReSharper disable once ClassNeverInstantiated.Local
    private record NameObj(string Name);
  }
  
}