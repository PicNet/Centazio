using Centazio.Core.Ctl;

namespace Centazio.Core.Runner;

public interface IFunction<T> where T : OperationConfig {

  FunctionConfig<T> Config { get; }
  Task<IEnumerable<OperationResult>> RunOperation(DateTime start, IOperationRunner<T> runner, ICtlRepository ctl);

}