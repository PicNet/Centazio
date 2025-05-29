using Centazio.Core;
using Centazio.Core.Secrets;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Cli.Infra.Az;

public interface IAzSubscriptions {
  Task<List<(string Id, string Name, string State)>> ListSubscriptions();
}

public class AzSubscriptions([FromKeyedServices(CentazioConstants.Hosts.Az)] CentazioSecrets secrets) : AbstractAzCommunicator(secrets), IAzSubscriptions {
  public Task<List<(string Id, string Name, string State)>> ListSubscriptions() {
    var subscriptions = GetClient().GetSubscriptions();
    return Task.FromResult(subscriptions.Select(s => (s.Id.Name, s.Data.DisplayName, s.Data.State?.ToString() ?? String.Empty)).ToList());
  }

  
}