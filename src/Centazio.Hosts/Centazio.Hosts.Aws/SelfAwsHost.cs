using Centazio.Core.Engine;
using Centazio.Core.Settings;
using Centazio.Hosts.Self;

namespace Centazio.Hosts.Aws;

public class SelfAwsHost : SelfHost {
  /*private Task SetupLocalAwsInfrastructure(ServiceProvider provider) {
    // TODO Setup LocalStack infrastructure if needed
    // throw new NotImplementedException();
    return Task.CompletedTask;
  }*/

  public async Task RunAwsHost(CentazioSettings settings, IHostConfiguration cmdsetts,  CentazioEngine centazio, bool uselocalaws) {
    if (uselocalaws) {
      Environment.SetEnvironmentVariable("AWS_ENDPOINT_URL", "http://localhost:4566");
      // TODO setup the local aws infrastructure
    }
    await RunHost(settings, cmdsetts, centazio);
  }

}