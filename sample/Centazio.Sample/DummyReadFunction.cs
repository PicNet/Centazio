using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Stage;

namespace Centazio.Sample;

public class DummyReadFunction(IStagedEntityRepository stager, ICtlRepository ctl) : ReadFunction(new(nameof(DummyReadFunction)), stager, ctl) {

  protected override FunctionConfig<ReadOperationConfig> GetFunctionConfiguration() => new([]);
  
  public override Task<ReadOperationResult> GetUpdatesAfterCheckpoint(OperationStateAndConfig<ReadOperationConfig> config) => throw new NotImplementedException();

}