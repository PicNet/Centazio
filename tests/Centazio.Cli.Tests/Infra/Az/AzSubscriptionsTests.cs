﻿using Centazio.Cli.Infra.Az;

namespace Centazio.Cli.Tests.Infra.Az;

public class AzSubscriptionsTests {

  [Test] public async Task Test_ListSubscriptions() {
    var az = new AzSubscriptions(TestingCliSecretsManager.Instance);
    var subs = await az.ListSubscriptions();
    Assert.That(subs, Is.Not.Empty);
  } 

}