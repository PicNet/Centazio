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

  public async Task<IEnumerable<(string Id, string Name, string Arn, string Status, string Email)>> ListAccounts() =>
      (await GetClient().ListAccountsAsync(new ListAccountsRequest()))
      .Accounts.Select(a => (a.Id, a.Name, a.Arn, a.Status.Value, a.Email));
  
  public async Task<string> AddAccount(string name) {
    if (1==1) return "";
    var response = await GetClient().CreateAccountAsync(new CreateAccountRequest { AccountName = name });
    var state = response.CreateAccountStatus.State;
    return state == CreateAccountState.SUCCEEDED ? "" : response.CreateAccountStatus.FailureReason.Value ?? "Unknown failure";
  }

  private AmazonOrganizationsClient GetClient() => new(new BasicAWSCredentials(secrets.AWS_KEY, secrets.AWS_SECRET),
      new AmazonOrganizationsConfig { RegionEndpoint = RegionEndpoint.APSoutheast2 });

}