namespace Centazio.Cli.Infra.Az;

public interface IAzSubscriptions {
  Task<IEnumerable<(string Id, string Name, string State)>> ListSubscriptions();
}

public class AzSubscriptions(CliSecrets secrets) : AbstractAzCommunicator(secrets), IAzSubscriptions {
  public Task<IEnumerable<(string Id, string Name, string State)>> ListSubscriptions() {
    var subscriptions = GetClient().GetSubscriptions();
    return Task.FromResult(subscriptions.Select(s => (s.Id.Name, s.Data.DisplayName, s.Data.State?.ToString() ?? "")));
  }

  
}