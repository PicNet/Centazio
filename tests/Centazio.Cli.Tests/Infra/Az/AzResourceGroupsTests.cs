﻿using Centazio.Cli.Infra.Az;

namespace Centazio.Cli.Tests.Infra.Az;

public class AzResourceGroupsTests {

  [Test] public async Task Test_ListResourceGroups() {
    var az = new AzResourceGroups(TestingFactories.Secrets());
    var rgs = await az.ListResourceGroups();
    Assert.That(rgs, Is.Not.Empty);
  } 

}