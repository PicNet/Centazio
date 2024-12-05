namespace Centazio.Core.Tests.Inspect;

public class InspectUtilsTests {

  [Test] public void Test_GetSolnFiles_csproj() {
    var csprojs = InspectUtils.GetSolnFiles(null, "*.csproj");
    Assert.That(csprojs.SingleOrDefault(f => f.EndsWith("Centazio.Core.csproj")), Is.Not.Null);
    Assert.That(csprojs.SingleOrDefault(f => f.EndsWith("Centazio.Sample.csproj")), Is.Not.Null);
    Assert.That(csprojs.SingleOrDefault(f => f.EndsWith("Centazio.Test.Lib.csproj")), Is.Not.Null);
  }

}