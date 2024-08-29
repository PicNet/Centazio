namespace Centazio.Cli.Infra;

public record CliSecrets(
    string AWS_KEY, 
    string AWS_SECRET,
    string AWS_REGION,
    string AZ_TENANT_ID, 
    string AZ_CLIENT_ID,
    string AZ_SECRET_ID,
    string AZ_SUBSCRIPTION_ID) {
  
  public CliSecrets() : this("", "", "", "", "", "", "") {}
}