using Azure.Identity;
using Azure.ResourceManager;
using Centazio.Core;
using Centazio.Core.Secrets;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Cli.Infra.Az;

public abstract class AbstractAzCommunicator([FromKeyedServices(CentazioConstants.Hosts.Az)] CentazioSecrets secrets) {
  
  protected CentazioSecrets Secrets => secrets;

  protected ArmClient GetClient() => new(new ClientSecretCredential(secrets.AZ_TENANT_ID, secrets.AZ_CLIENT_ID, secrets.AZ_SECRET_ID));

}