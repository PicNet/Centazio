using Amazon;
using Amazon.Organizations;
using Amazon.Organizations.Model;
using Amazon.Runtime;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;

namespace Centazio.Cli.Infra.Aws;

public interface IAwsAccounts {

  Task<List<(string Id, string Name, string Arn, string Status, string Email)>> ListAccounts();
  Task<string> AddAccount(string name);

}

public class AwsAccounts(CentazioSecrets secrets, AwsSettings settings) : IAwsAccounts {

  private readonly IAmazonOrganizations client = new AmazonOrganizationsClient(
      new BasicAWSCredentials(secrets.AWS_KEY, secrets.AWS_SECRET),
      new AmazonOrganizationsConfig { RegionEndpoint = RegionEndpoint.GetBySystemName(settings.Region) });

  public async Task<List<(string Id, string Name, string Arn, string Status, string Email)>> ListAccounts() =>
      (await client.ListAccountsAsync(new ListAccountsRequest()))
      .Accounts.Select(a => (a.Id, a.Name, a.Arn, a.Status.Value, a.Email))
      .ToList();
  
  public async Task<string> AddAccount(string name) {
    var response = await client.CreateAccountAsync(new CreateAccountRequest { AccountName = name });
    var state = response.CreateAccountStatus.State;
    return state == CreateAccountState.SUCCEEDED ? String.Empty : response.CreateAccountStatus.FailureReason.Value ?? "Unknown failure";
  }

}