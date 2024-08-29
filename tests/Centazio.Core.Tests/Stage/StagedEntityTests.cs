using Centazio.Core.Ctl.Entities;

namespace Centazio.Core.Tests.Stage;

public class StagedEntityTests {

  private static readonly string NAME = nameof(StagedEntityTests);
  
  [Test] public void Test_initialisation_handles_ignore_correctly() {
    Assert.That(new StagedEntity(NAME, NAME, DateTime.UtcNow, "", Ignore: null).Ignore, Is.Null);
    Assert.That(new StagedEntity(NAME, NAME, DateTime.UtcNow, "", Ignore: "").Ignore, Is.Null);
    Assert.That(new StagedEntity(NAME, NAME, DateTime.UtcNow, "", Ignore: " ").Ignore, Is.Null);
    Assert.That(new StagedEntity(NAME, NAME, DateTime.UtcNow, "", Ignore: "\n\t ").Ignore, Is.Null);
  }
  
  [Test] public void Test_subclass_initialisation_handles_ignore_correctly() {
    Assert.That(new SubClassSE().Ignore, Is.Null);
    Assert.That(new SubClassSE("").Ignore, Is.Null);
    Assert.That(new SubClassSE(" ").Ignore, Is.Null);
    Assert.That(new SubClassSE("\n\t ").Ignore, Is.Null);
  }
  
  [Test] public void Test_subclass_obj_initialisers_handles_ignore_correctly() {
    Assert.That(new SubClassSE { Ignore = null }.Ignore, Is.Null);
    Assert.That(new SubClassSE { Ignore = "" }.Ignore, Is.Null);
    Assert.That(new SubClassSE { Ignore = " " }.Ignore, Is.Null);
    Assert.That(new SubClassSE { Ignore = "\n\t " }.Ignore, Is.Null);
  }

  [Test] public void Test_subclass_with_syntax_handles_ignore_correctly() {
    // ReSharper disable WithExpressionInsteadOfInitializer
    Assert.That((new SubClassSE() with { Ignore = null }).Ignore, Is.Null);
    Assert.That((new SubClassSE() with { Ignore = "" }).Ignore, Is.Null);
    Assert.That((new SubClassSE() with { Ignore = " " }).Ignore, Is.Null);
    Assert.That((new SubClassSE() with { Ignore = "\n\t " }).Ignore, Is.Null);
  }
  
  private record SubClassSE(string? Ignore = null) : StagedEntity(NAME, NAME, DateTime.UtcNow, "", Ignore: Ignore);
}