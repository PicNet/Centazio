namespace Centazio.Core.Secrets;

public record CentazioSecrets {
  
  // todo GT: aws and azure is required, this should not be the case and should be dependent on usage
  public string AWS_KEY { get; set; } 
  public string AWS_SECRET { get; set; }
  
  public string AZ_TENANT_ID { get; set; } 
  public string AZ_CLIENT_ID { get; set; }
  public string AZ_SECRET_ID { get; set; }
  public string AZ_SUBSCRIPTION_ID { get; set; }
  public string AZ_BLOB_STORAGE_ENDPOINT { get; set; }
  public string? AZ_APP_INSIGHT_CONNECTION_STRING { get; set; }
  
  // todo GT: sql secrets are only required when a provider requires it.  And the settings
  //    should point to the connstr.  In fact connection strings should live in settings and read only
  //    secrets from secrets store, so we need to inject these values into settings
  public string? SQL_CONN_STR { get; set; }
  // todo GT: nuget is only for dev cli commands and should not be here
  public string? NUGET_API_KEY { get; set; }
  
  public CentazioSecrets() : this (null!, null!, null!, null!, null!, null!, null!, null!, null!, null!) {}
  
  protected CentazioSecrets(CentazioSecrets other) {
    AWS_KEY = other.AWS_KEY;
    AWS_SECRET = other.AWS_SECRET;
    AZ_TENANT_ID = other.AZ_TENANT_ID;
    AZ_CLIENT_ID = other.AZ_CLIENT_ID;
    AZ_SECRET_ID = other.AZ_SECRET_ID;
    AZ_SUBSCRIPTION_ID = other.AZ_SUBSCRIPTION_ID;
    AZ_BLOB_STORAGE_ENDPOINT = other.AZ_BLOB_STORAGE_ENDPOINT;
    AZ_APP_INSIGHT_CONNECTION_STRING = other.AZ_APP_INSIGHT_CONNECTION_STRING;
    SQL_CONN_STR = other.SQL_CONN_STR;
    NUGET_API_KEY = other.NUGET_API_KEY;
  }
  
  private CentazioSecrets(
      string AWS_KEY, 
      string AWS_SECRET, 
      string AZ_TENANT_ID, 
      string AZ_CLIENT_ID, 
      string AZ_SECRET_ID, 
      string AZ_SUBSCRIPTION_ID, 
      string AZ_BLOB_STORAGE_ENDPOINT, 
      string? AZ_APP_INSIGHT_CONNECTION_STRING, 
      string? SQL_CONN_STR,
      string? NUGET_API_KEY) {
    this.AWS_KEY = AWS_KEY;
    this.AWS_SECRET = AWS_SECRET;
    this.AZ_TENANT_ID = AZ_TENANT_ID;
    this.AZ_CLIENT_ID = AZ_CLIENT_ID;
    this.AZ_SECRET_ID = AZ_SECRET_ID;
    this.AZ_SUBSCRIPTION_ID = AZ_SUBSCRIPTION_ID;
    this.AZ_BLOB_STORAGE_ENDPOINT = AZ_BLOB_STORAGE_ENDPOINT;
    this.AZ_APP_INSIGHT_CONNECTION_STRING = AZ_APP_INSIGHT_CONNECTION_STRING;
    this.SQL_CONN_STR = SQL_CONN_STR;
    this.NUGET_API_KEY = NUGET_API_KEY;
  }
}