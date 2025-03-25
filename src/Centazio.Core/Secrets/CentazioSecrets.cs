namespace Centazio.Core.Secrets;

public record CentazioSecrets {
  
  public string AWS_KEY { get; } 
  public string AWS_SECRET { get; }
  public string AWS_REGION { get; }
  
  public string AZ_TENANT_ID { get; } 
  public string AZ_CLIENT_ID { get; }
  public string AZ_SECRET_ID { get; }
  public string AZ_SUBSCRIPTION_ID { get; }
  public string AZ_APP_INSIGHT_CONNECTION_STRING { get; }
  public string AZ_BLOB_STORAGE_ENDPOINT { get; }
  
  public string? SQL_CONN_STR { get; init; }
  
  protected CentazioSecrets(CentazioSecrets other) {
    AWS_KEY = other.AWS_KEY;
    AWS_SECRET = other.AWS_SECRET;
    AWS_REGION = other.AWS_REGION;
    AZ_TENANT_ID = other.AZ_TENANT_ID;
    AZ_CLIENT_ID = other.AZ_CLIENT_ID;
    AZ_SECRET_ID = other.AZ_SECRET_ID;
    AZ_SUBSCRIPTION_ID = other.AZ_SUBSCRIPTION_ID;
    AZ_BLOB_STORAGE_ENDPOINT = other.AZ_BLOB_STORAGE_ENDPOINT;
    AZ_APP_INSIGHT_CONNECTION_STRING = other.AZ_APP_INSIGHT_CONNECTION_STRING;
    SQL_CONN_STR = other.SQL_CONN_STR;
  }
  
  private CentazioSecrets(string AWS_KEY, string AWS_SECRET, string AWS_REGION, string AZ_TENANT_ID, string AZ_CLIENT_ID, string AZ_SECRET_ID, string AZ_SUBSCRIPTION_ID, string AZ_BLOB_STORAGE_ENDPOINT, string AZ_APP_INSIGHT_CONNECTION_STRING, string SQL_CONN_STR) {
    this.AWS_KEY = AWS_KEY;
    this.AWS_SECRET = AWS_SECRET;
    this.AWS_REGION = AWS_REGION;
    this.AZ_TENANT_ID = AZ_TENANT_ID;
    this.AZ_CLIENT_ID = AZ_CLIENT_ID;
    this.AZ_SECRET_ID = AZ_SECRET_ID;
    this.AZ_SUBSCRIPTION_ID = AZ_SUBSCRIPTION_ID;
    this.AZ_BLOB_STORAGE_ENDPOINT = AZ_BLOB_STORAGE_ENDPOINT;
    this.AZ_APP_INSIGHT_CONNECTION_STRING = AZ_APP_INSIGHT_CONNECTION_STRING;
    this.SQL_CONN_STR = SQL_CONN_STR;
  }
  
  public record Dto : IDto<CentazioSecrets> {
    public string? AWS_KEY { get; set; } 
    public string? AWS_SECRET { get; set; }
    public string? AWS_REGION { get; set; }
        
    public string? AZ_TENANT_ID { get; set; }
    public string? AZ_CLIENT_ID { get; set; }
    public string? AZ_SECRET_ID { get; set; }
    public string? AZ_SUBSCRIPTION_ID { get; set; }
    public string? AZ_APP_INSIGHT_CONNECTION_STRING { get; set; }
    public string? AZ_BLOB_STORAGE_ENDPOINT { get; set; }
    
    public string? SQL_CONN_STR { get; set; }
    
    public CentazioSecrets ToBase() => new(
      String.IsNullOrWhiteSpace(AWS_KEY) ? throw new ArgumentNullException(nameof(AWS_KEY)) : AWS_KEY.Trim(),
      String.IsNullOrWhiteSpace(AWS_SECRET) ? throw new ArgumentNullException(nameof(AWS_SECRET)) : AWS_SECRET.Trim(),
      String.IsNullOrWhiteSpace(AWS_REGION) ? throw new ArgumentNullException(nameof(AWS_REGION)) : AWS_REGION.Trim(),
      
      String.IsNullOrWhiteSpace(AZ_TENANT_ID) ? throw new ArgumentNullException(nameof(AZ_TENANT_ID)) : AZ_TENANT_ID.Trim(),
      String.IsNullOrWhiteSpace(AZ_CLIENT_ID) ? throw new ArgumentNullException(nameof(AZ_CLIENT_ID)) : AZ_CLIENT_ID.Trim(),
      String.IsNullOrWhiteSpace(AZ_SECRET_ID) ? throw new ArgumentNullException(nameof(AZ_SECRET_ID)) : AZ_SECRET_ID.Trim(),
      String.IsNullOrWhiteSpace(AZ_SUBSCRIPTION_ID) ? throw new ArgumentNullException(nameof(AZ_SUBSCRIPTION_ID)) : AZ_SUBSCRIPTION_ID.Trim(),
      String.IsNullOrWhiteSpace(AZ_BLOB_STORAGE_ENDPOINT) ? throw new ArgumentNullException(nameof(AZ_BLOB_STORAGE_ENDPOINT)) : AZ_BLOB_STORAGE_ENDPOINT.Trim(),
      String.IsNullOrWhiteSpace(AZ_APP_INSIGHT_CONNECTION_STRING) ? throw new ArgumentNullException(nameof(AZ_APP_INSIGHT_CONNECTION_STRING)) : AZ_APP_INSIGHT_CONNECTION_STRING.Trim(),
      
      String.IsNullOrWhiteSpace(SQL_CONN_STR) ? throw new ArgumentNullException(nameof(SQL_CONN_STR)) : SQL_CONN_STR.Trim());
  }
}