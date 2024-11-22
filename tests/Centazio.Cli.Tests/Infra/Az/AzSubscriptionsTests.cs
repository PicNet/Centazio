using Centazio.Cli.Infra.Az;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests.Infra.Az;

public class AzSubscriptionsTests {

  [Test] public async Task Test_ListSubscriptions() {
    var az = new AzSubscriptions(TestingFactories.Secrets());
    var subs = await az.ListSubscriptions();
    Assert.That(subs, Is.Not.Empty);
  } 

}