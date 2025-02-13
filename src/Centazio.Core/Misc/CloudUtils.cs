namespace Centazio.Core.Misc;

public class CloudUtils {

  public static bool IsCloudEnviornment() => !String.IsNullOrEmpty(Environment.GetEnvironmentVariable("FUNCTIONS_WORKER_RUNTIME"));

}