using Azure.Identity;
using Azure.ResourceManager;
using Centazio.Core.Secrets;

namespace Centazio.Cli.Infra.Az;

public abstract class AbstractAzCommunicator(ICliSecretsManager loader) {
  
  private CentazioSecrets? secrets;
  private ArmClient? client;
  
  protected async Task<CentazioSecrets> GetSecrets() {
    if (secrets is not null) return secrets;
    return secrets = await loader.LoadSecrets<CentazioSecrets>(CentazioConstants.Hosts.Az);
  }
  
  protected async Task<ArmClient> GetClient() {
    if (client is not null) return client;
    var sec = await GetSecrets();
    return client = new(new ClientSecretCredential(sec.AZ_TENANT_ID, sec.AZ_CLIENT_ID, sec.AZ_SECRET_ID));
  }

}