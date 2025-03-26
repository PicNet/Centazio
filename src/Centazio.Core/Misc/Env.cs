namespace Centazio.Core.Misc;

public class Env {

  public static bool IsCloudEnviornment() => !String.IsNullOrEmpty(Environment.GetEnvironmentVariable("FUNCTIONS_WORKER_RUNTIME"));
  
  public static bool IsGitHubActions() => Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true";

}