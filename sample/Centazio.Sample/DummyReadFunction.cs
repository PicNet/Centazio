using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Stage;

namespace Centazio.Sample;

public class DummyReadFunction(IStagedEntityRepository stager, ICtlRepository ctl) : AbstractFunction<ReadOperationConfig, ReadOperationResult>(new ReadOperationRunner(stager), ctl) {

  protected override FunctionConfig<ReadOperationConfig> Config { get; } = new(new(nameof(DummyReadFunction)), LifecycleStage.Defaults.Read, []);

}