namespace Centazio.Core;

public static class CentazioConstants {

  public const string DEFAULT_ENVIRONMENT = "dev";
  public const string SETTINGS_FILE_NAME = "settings.json";
  public const string ENV_SETTINGS_FILE_NAME = "settings.<environment>.json";
  public const string DEFAULTS_SETTINGS_FILE_NAME = "settings.defaults.json";

  public static class Hosts {
    public const string Aws = "aws";
    public const string Az = "az";
    public const string Self = "self";
  }
  
}