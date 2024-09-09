using Centazio.Core.Ctl;

namespace Centazio.Core.Runner;

public interface IFunction<T, R> 
    where T : OperationConfig 
    where R : OperationResult {

  FunctionConfig<T> Config { get; }
  Task<IEnumerable<R>> RunOperation(DateTime start, IOperationRunner<T, R> runner, ICtlRepository ctl);

}