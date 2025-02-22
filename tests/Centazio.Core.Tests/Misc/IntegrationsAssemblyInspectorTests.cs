using Centazio.Core.Ctl;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Stage;

namespace Centazio.Core.Tests.Misc;

public class IntegrationsAssemblyInspectorTests {

  [Test] public void Test_GetCentazioFunctions() {
    var funcs = IntegrationsAssemblyInspector.GetCentazioFunctions(GetType().Assembly, []);
    
    Assert.That(funcs, Does.Contain(typeof(IntegrationsAssemblyInspectorTests_ReadFunction)));
    Assert.That(funcs.Distinct().Count(), Is.EqualTo(funcs.Count));
  }

}

public class IntegrationsAssemblyInspectorTests_ReadFunction(SystemName system, IEntityStager stager, ICtlRepository ctl) : ReadFunction(system, stager, ctl, F.Settings()) {

  protected override FunctionConfig<ReadOperationConfig> GetFunctionConfiguration() => throw new Exception();

}