using Amazon;
using Amazon.Organizations;
using Amazon.Organizations.Model;
using Amazon.Runtime;

namespace Centazio.Cli.Infra.Aws;

public interface IAwsAccounts {

  Task<IEnumerable<(string Id, string Name, string Arn, string Status, string Email)>> ListAccounts();
  Task<string> AddAccount(string name);

}

public class AwsAccounts(CliSecrets secrets) : IAwsAccounts {

  private readonly IAmazonOrganizations client = new AmazonOrganizationsClient(
      new BasicAWSCredentials(secrets.AWS_KEY, secrets.AWS_SECRET),
      new AmazonOrganizationsConfig { RegionEndpoint = RegionEndpoint.GetBySystemName(secrets.AWS_REGION) });

  public async Task<IEnumerable<(string Id, string Name, string Arn, string Status, string Email)>> ListAccounts() =>
      (await client.ListAccountsAsync(new ListAccountsRequest()))
      .Accounts.Select(a => (a.Id, a.Name, a.Arn, a.Status.Value, a.Email));
  
  public async Task<string> AddAccount(string name) {
    var response = await client.CreateAccountAsync(new CreateAccountRequest { AccountName = name });
    var state = response.CreateAccountStatus.State;
    return state == CreateAccountState.SUCCEEDED ? "" : response.CreateAccountStatus.FailureReason.Value ?? "Unknown failure";
  }

}