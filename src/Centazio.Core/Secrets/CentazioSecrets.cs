namespace Centazio.Core.Secrets;

public record CentazioSecrets(
    string AWS_KEY, 
    string AWS_SECRET,
    string AWS_REGION,
    
    string AZ_TENANT_ID, 
    string AZ_CLIENT_ID,
    string AZ_SECRET_ID,
    string AZ_SUBSCRIPTION_ID
) {
  
  public CentazioSecrets() : this("", "", "", "", "", "", "") {}
}