using Centazio.Cli.Infra.Aws;

namespace Centazio.Cli.Tests.Infra.Aws;

// AwsAccounts cannot be tested using Testcontainers and is too dangerous to test non-readonly methods here
public class AwsAccountsTests {
  
 [Test, Ignore("additional permissions required for token which is not worth adding for this test")] public async Task Test_ListAccounts() {
   var impl = new AwsAccounts(TestingCliSecretsManager.Instance, (await F.Settings()).AwsSettings);
   var accounts = await impl.ListAccounts();
   Assert.That(accounts, Is.Not.Empty);
 } 

}