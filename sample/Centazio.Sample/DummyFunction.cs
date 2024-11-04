using Centazio.Core;
using Centazio.Core.Read;
using Centazio.Core.Runner;

namespace Centazio.Sample;

public class DummyFunction : AbstractFunction<ReadOperationConfig, ReadOperationResult> {

  public override FunctionConfig<ReadOperationConfig> Config { get; } = new(new(nameof(DummyFunction)), LifecycleStage.Defaults.Read, []);

}