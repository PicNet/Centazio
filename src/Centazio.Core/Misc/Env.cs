using System.Runtime.InteropServices;

namespace Centazio.Core.Misc;

public static class Env {

  public static bool IsSelfHost => VarIsTrue("CENTAZIO_HOST");
  public static bool IsCloudHost => ContainsVar("FUNCTIONS_WORKER_RUNTIME") || ContainsVar("AWS_LAMBDA_FUNCTION_NAME"); 
  public static bool IsHostedEnv => IsSelfHost || IsCloudHost;
  
  public static bool IsGitHubActions => VarIsTrue("GITHUB_ACTIONS");
  public static bool IsUnitTest => VarIsTrue("IS_UNIT_TEST");
  public static bool IsCli => VarIsTrue("IS_CLI");
  public static bool IsInDev => FsUtils.FindFileDirectory(FsUtils.TEST_DEV_FILE) is not null;
  public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
  
  private static string Var(string varname) => Environment.GetEnvironmentVariable(varname)?.Trim() ?? String.Empty;
  private static bool ContainsVar(string varname) => Var(varname) != String.Empty;
  private static bool VarIsTrue(string varname) => Var(varname) == "true";
}