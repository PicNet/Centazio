using Amazon;
using Amazon.Runtime;
using Centazio.Core.Secrets;

namespace Centazio.Cli.Infra.Aws;

public abstract class AbstractAwsCommunicator(ICliSecretsManager loader, AwsSettings settings) {

  private CentazioSecrets? secrets;
  
  protected async Task<CentazioSecrets> GetSecrets() {
    if (secrets is not null) return secrets;
    return secrets ??= await loader.LoadSecrets<CentazioSecrets>(CentazioConstants.Hosts.Aws);
  }
  
  protected async Task<BasicAWSCredentials> GetCredentials() {
    var sec = await GetSecrets();
    return new BasicAWSCredentials(sec.AWS_KEY, sec.AWS_SECRET);
  }
  
  protected RegionEndpoint GetRegion() => RegionEndpoint.GetBySystemName(settings.Region);

}