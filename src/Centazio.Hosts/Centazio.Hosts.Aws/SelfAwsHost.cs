using Centazio.Core.Engine;
using Centazio.Core.Settings;
using Centazio.Hosts.Self;

namespace Centazio.Hosts.Aws;

public class SelfAwsHost : SelfHost {
  public async Task RunAwsHost(CentazioSettings settings, IHostConfiguration cmdsetts,  CentazioEngine centazio) {
    await RunHost(settings, cmdsetts, centazio);
  }

}