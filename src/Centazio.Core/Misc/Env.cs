namespace Centazio.Core.Misc;

public class Env {

  public static bool IsHostedEnv() => 
      Environment.GetEnvironmentVariable("CENTAZIO_HOST") == "true" 
      || !String.IsNullOrEmpty(Environment.GetEnvironmentVariable("FUNCTIONS_WORKER_RUNTIME")) 
      || !String.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME"));
  public static bool IsGitHubActions() => Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true";
  public static bool IsUnitTest() => Environment.GetEnvironmentVariable("IS_UNIT_TEST") == "true";
  public static bool IsCli() => Environment.GetEnvironmentVariable("IS_CLI") == "true";
  public static bool IsInDev() => FsUtils.FindFileDirectory(FsUtils.TEST_DEV_FILE) is not null;
}