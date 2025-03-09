using Centazio.Core.Runner;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.Misc;

public class IntegrationsAssemblyInspectorTests {

  [Test] public void Test_GetCentazioFunctions() {
    var funcs = IntegrationsAssemblyInspector.GetCentazioFunctions(typeof(EmptyReadFunction).Assembly, []);
    
    Assert.That(funcs, Does.Contain(typeof(EmptyReadFunction)));
    Assert.That(funcs, Does.Contain(typeof(EmptyPromoteFunction)));
    Assert.That(funcs.Distinct().Count(), Is.EqualTo(funcs.Count));
  }
}