namespace Centazio.Core.Secrets;

public record CentazioSecretsRaw {
  public string? AWS_KEY { get; init; } 
  public string? AWS_SECRET { get; init; }
  public string? AWS_REGION { get; init; }
      
  public string? AZ_TENANT_ID { get; init; }
  public string? AZ_CLIENT_ID { get; init; }
  public string? AZ_SECRET_ID { get; init; }
  public string? AZ_SUBSCRIPTION_ID { get; init; }
  
  public static explicit operator CentazioSecrets(CentazioSecretsRaw raw) => new(
    raw.AWS_KEY ?? throw new ArgumentNullException(nameof(AWS_KEY)),
    raw.AWS_SECRET ?? throw new ArgumentNullException(nameof(AWS_SECRET)),
    raw.AWS_REGION ?? throw new ArgumentNullException(nameof(AWS_REGION)),
    
    raw.AZ_TENANT_ID ?? throw new ArgumentNullException(nameof(AZ_TENANT_ID)),
    raw.AZ_CLIENT_ID ?? throw new ArgumentNullException(nameof(AZ_CLIENT_ID)),
    raw.AZ_SECRET_ID ?? throw new ArgumentNullException(nameof(AZ_SECRET_ID)),
    raw.AZ_SUBSCRIPTION_ID ?? throw new ArgumentNullException(nameof(AZ_SUBSCRIPTION_ID))); 
}

public record CentazioSecrets(
    string AWS_KEY, 
    string AWS_SECRET,
    string AWS_REGION,
    
    string AZ_TENANT_ID, 
    string AZ_CLIENT_ID,
    string AZ_SECRET_ID,
    string AZ_SUBSCRIPTION_ID);