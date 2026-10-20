using Centazio.Cli.Infra.Az;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests.Infra.Az;

public class AzSubscriptionsTests {

  [OneTimeSetUp] public Task Init() => AzTestingEnv.IgnoreIfVaultUnreachable();

  [Test] public async Task Test_ListSubscriptions() {
    var az = new AzSubscriptions(TestingCliSecretsManager.Instance);
    var subs = await az.ListSubscriptions();
    Assert.That(subs, Is.Not.Empty);
  } 

}