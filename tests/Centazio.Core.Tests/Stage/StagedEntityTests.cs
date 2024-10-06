﻿using Centazio.Core.Ctl.Entities;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.Stage;

public class StagedEntityTests {

  private static readonly string NAME = nameof(StagedEntityTests);
  
  [Test] public void Test_initialisation_handles_ignore_correctly() {
    Assert.That(((StagedEntity) new StagedEntity.Dto(Guid.CreateVersion7(), NAME, NAME, UtcDate.UtcNow, NAME, Helpers.TestingChecksum(NAME), ignoreres: null)).IgnoreReason, Is.Null);
    Assert.That(((StagedEntity) new StagedEntity.Dto(Guid.CreateVersion7(), NAME, NAME, UtcDate.UtcNow, NAME, Helpers.TestingChecksum(NAME), ignoreres: String.Empty)).IgnoreReason, Is.Null);
    Assert.That(((StagedEntity) new StagedEntity.Dto(Guid.CreateVersion7(), NAME, NAME, UtcDate.UtcNow, NAME, Helpers.TestingChecksum(NAME), ignoreres: " ")).IgnoreReason, Is.Null);
    Assert.That(((StagedEntity) new StagedEntity.Dto(Guid.CreateVersion7(), NAME, NAME, UtcDate.UtcNow, NAME, Helpers.TestingChecksum(NAME), ignoreres: "\n\t ")).IgnoreReason, Is.Null);
  }
}