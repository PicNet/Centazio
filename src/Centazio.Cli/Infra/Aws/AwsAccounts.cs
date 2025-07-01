using Amazon.Organizations;
using Amazon.Organizations.Model;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Cli.Infra.Aws;

public interface IAwsAccounts {

  Task<List<(string Id, string Name, string Arn, string Status, string Email)>> ListAccounts();
  Task<string> AddAccount(string name);

}

public class AwsAccounts(ICliSecretsManager secrets, [FromKeyedServices(CentazioConstants.Hosts.Aws)] AwsSettings settings) : AbstractAwsCommunicator(secrets, settings), IAwsAccounts {

  public async Task<List<(string Id, string Name, string Arn, string Status, string Email)>> ListAccounts() {
    var client = new AmazonOrganizationsClient(await GetCredentials(), GetRegion());
    return (await client.ListAccountsAsync(new ListAccountsRequest()))
        .Accounts.Select(a => (a.Id, a.Name, a.Arn, a.Status.Value, a.Email))
        .ToList();
  }

  public async Task<string> AddAccount(string name) {
    var client = new AmazonOrganizationsClient(await GetCredentials(), GetRegion());
    var response = await client.CreateAccountAsync(new CreateAccountRequest { AccountName = name });
    var state = response.CreateAccountStatus.State;
    return state == CreateAccountState.SUCCEEDED ? String.Empty : response.CreateAccountStatus.FailureReason.Value ?? "Unknown failure";
  }
}