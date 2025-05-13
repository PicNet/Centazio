using Centazio.Cli.Infra.Az;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests.Infra.Az;

public class AzResourceGroupsTests {

  [Test] public async Task Test_ListResourceGroups() {
    var az = new AzResourceGroups(await TestingFactories.Secrets());
    var rgs = await az.ListResourceGroups();
    Assert.That(rgs, Is.Not.Empty);
  } 

}