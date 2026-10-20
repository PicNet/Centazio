using Centazio.Cli.Infra.Az;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests.Infra.Az;

public class AzResourceGroupsTests {

  [OneTimeSetUp] public Task Init() => AzTestingEnv.IgnoreIfVaultUnreachable();

  [Test] public async Task Test_ListResourceGroups() {
    var az = new AzResourceGroups(TestingCliSecretsManager.Instance);
    var rgs = await az.ListResourceGroups();
    Assert.That(rgs, Is.Not.Empty);
  } 

}