namespace Centazio.Cli.Infra.Az;

public interface IAzSubscriptions {
  Task<List<(string Id, string Name, string State)>> ListSubscriptions();
}

public class AzSubscriptions(ICliSecretsManager loader) : AbstractAzCommunicator(loader), IAzSubscriptions {
  public async Task<List<(string Id, string Name, string State)>> ListSubscriptions() {
    var subscriptions = (await GetClient()).GetSubscriptions();
    return subscriptions.Select(s => (s.Id.Name, s.Data.DisplayName, s.Data.State?.ToString() ?? String.Empty)).ToList();
  }

  
}