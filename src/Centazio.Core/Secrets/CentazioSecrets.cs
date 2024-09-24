namespace Centazio.Core.Secrets;

public record CentazioSecrets {
  
  public string AWS_KEY { get; } 
  public string AWS_SECRET { get; }
  public string AWS_REGION { get; }
  public string AZ_TENANT_ID { get; } 
  public string AZ_CLIENT_ID { get; }
  public string AZ_SECRET_ID { get; }
  public string AZ_SUBSCRIPTION_ID { get; }
  
  private CentazioSecrets(string AWS_KEY, string AWS_SECRET, string AWS_REGION, string AZ_TENANT_ID, string AZ_CLIENT_ID, string AZ_SECRET_ID, string AZ_SUBSCRIPTION_ID) {
    this.AWS_KEY = AWS_KEY;
    this.AWS_SECRET = AWS_SECRET;
    this.AWS_REGION = AWS_REGION;
    this.AZ_TENANT_ID = AZ_TENANT_ID;
    this.AZ_CLIENT_ID = AZ_CLIENT_ID;
    this.AZ_SECRET_ID = AZ_SECRET_ID;
    this.AZ_SUBSCRIPTION_ID = AZ_SUBSCRIPTION_ID;
  }
  
  public record Dto {
    public string? AWS_KEY { get; init; } 
    public string? AWS_SECRET { get; init; }
    public string? AWS_REGION { get; init; }
        
    public string? AZ_TENANT_ID { get; init; }
    public string? AZ_CLIENT_ID { get; init; }
    public string? AZ_SECRET_ID { get; init; }
    public string? AZ_SUBSCRIPTION_ID { get; init; }
    
    public static explicit operator CentazioSecrets(Dto dto) => new(
      String.IsNullOrWhiteSpace(dto.AWS_KEY) ? throw new ArgumentNullException(nameof(AWS_KEY)) : dto.AWS_KEY.Trim(),
      String.IsNullOrWhiteSpace(dto.AWS_SECRET) ? throw new ArgumentNullException(nameof(AWS_SECRET)) : dto.AWS_SECRET.Trim(),
      String.IsNullOrWhiteSpace(dto.AWS_REGION) ? throw new ArgumentNullException(nameof(AWS_REGION)) : dto.AWS_REGION.Trim(),
      String.IsNullOrWhiteSpace(dto.AZ_TENANT_ID) ? throw new ArgumentNullException(nameof(AZ_TENANT_ID)) : dto.AZ_TENANT_ID.Trim(),
      String.IsNullOrWhiteSpace(dto.AZ_CLIENT_ID) ? throw new ArgumentNullException(nameof(AZ_CLIENT_ID)) : dto.AZ_CLIENT_ID.Trim(),
      String.IsNullOrWhiteSpace(dto.AZ_SECRET_ID) ? throw new ArgumentNullException(nameof(AZ_SECRET_ID)) : dto.AZ_SECRET_ID.Trim(),
      String.IsNullOrWhiteSpace(dto.AZ_SUBSCRIPTION_ID) ? throw new ArgumentNullException(nameof(AZ_SUBSCRIPTION_ID)) : dto.AZ_SUBSCRIPTION_ID.Trim()); 
  }
}