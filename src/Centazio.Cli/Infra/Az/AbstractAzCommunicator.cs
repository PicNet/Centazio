using Azure.Identity;
using Azure.ResourceManager;

namespace Centazio.Cli.Infra.Az;

public abstract class AbstractAzCommunicator(CliSecrets secrets) {
  
  protected CliSecrets Secrets => secrets;

  protected ArmClient GetClient() => new(new ClientSecretCredential(secrets.AZ_TENANT_ID, secrets.AZ_CLIENT_ID, secrets.AZ_SECRET_ID));

}