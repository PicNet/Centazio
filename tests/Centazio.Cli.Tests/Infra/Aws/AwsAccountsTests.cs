using Centazio.Cli.Infra.Aws;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests.Infra.Aws;

// AwsAccounts cannot be tested using Testcontainers and is too dangerous to test non-readonly methods here
public class AwsAccountsTests {
  
 [Test] public async Task Test_ListAccounts() {
   var impl = new AwsAccounts(TestingFactories.Secrets());
   var accounts = await impl.ListAccounts();
   Assert.That(accounts, Is.Not.Empty);
 } 

}