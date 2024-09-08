namespace Centazio.Core.Runner;

public interface IOperationRunner<T, R> 
    where T : OperationConfig 
    where R : IOperationResult {
  Task<R> RunOperation(DateTime funcstart, OperationStateAndConfig<T> op);
}