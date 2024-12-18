namespace Centazio.Core.Secrets;

public record CentazioSecrets {
  
  public string AWS_KEY { get; } 
  public string AWS_SECRET { get; }
  public string AWS_REGION { get; }
  
  public string AZ_TENANT_ID { get; } 
  public string AZ_CLIENT_ID { get; }
  public string AZ_SECRET_ID { get; }
  public string AZ_SUBSCRIPTION_ID { get; }
  
  protected CentazioSecrets(CentazioSecrets other) {
    AWS_KEY = other.AWS_KEY;
    AWS_SECRET = other.AWS_SECRET;
    AWS_REGION = other.AWS_REGION;
    AZ_TENANT_ID = other.AZ_TENANT_ID;
    AZ_CLIENT_ID = other.AZ_CLIENT_ID;
    AZ_SECRET_ID = other.AZ_SECRET_ID;
    AZ_SUBSCRIPTION_ID = other.AZ_SUBSCRIPTION_ID;
  }
  
  private CentazioSecrets(string AWS_KEY, string AWS_SECRET, string AWS_REGION, string AZ_TENANT_ID, string AZ_CLIENT_ID, string AZ_SECRET_ID, string AZ_SUBSCRIPTION_ID) {
    this.AWS_KEY = AWS_KEY;
    this.AWS_SECRET = AWS_SECRET;
    this.AWS_REGION = AWS_REGION;
    this.AZ_TENANT_ID = AZ_TENANT_ID;
    this.AZ_CLIENT_ID = AZ_CLIENT_ID;
    this.AZ_SECRET_ID = AZ_SECRET_ID;
    this.AZ_SUBSCRIPTION_ID = AZ_SUBSCRIPTION_ID;
  }
  
  public record Dto : IDto<CentazioSecrets> {
    public string? AWS_KEY { get; set; } 
    public string? AWS_SECRET { get; set; }
    public string? AWS_REGION { get; set; }
        
    public string? AZ_TENANT_ID { get; set; }
    public string? AZ_CLIENT_ID { get; set; }
    public string? AZ_SECRET_ID { get; set; }
    public string? AZ_SUBSCRIPTION_ID { get; set; }
    
    public CentazioSecrets ToBase() => new(
      String.IsNullOrWhiteSpace(AWS_KEY) ? throw new ArgumentNullException(nameof(AWS_KEY)) : AWS_KEY.Trim(),
      String.IsNullOrWhiteSpace(AWS_SECRET) ? throw new ArgumentNullException(nameof(AWS_SECRET)) : AWS_SECRET.Trim(),
      String.IsNullOrWhiteSpace(AWS_REGION) ? throw new ArgumentNullException(nameof(AWS_REGION)) : AWS_REGION.Trim(),
      
      String.IsNullOrWhiteSpace(AZ_TENANT_ID) ? throw new ArgumentNullException(nameof(AZ_TENANT_ID)) : AZ_TENANT_ID.Trim(),
      String.IsNullOrWhiteSpace(AZ_CLIENT_ID) ? throw new ArgumentNullException(nameof(AZ_CLIENT_ID)) : AZ_CLIENT_ID.Trim(),
      String.IsNullOrWhiteSpace(AZ_SECRET_ID) ? throw new ArgumentNullException(nameof(AZ_SECRET_ID)) : AZ_SECRET_ID.Trim(),
      String.IsNullOrWhiteSpace(AZ_SUBSCRIPTION_ID) ? throw new ArgumentNullException(nameof(AZ_SUBSCRIPTION_ID)) : AZ_SUBSCRIPTION_ID.Trim());
  }
}