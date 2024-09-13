namespace Centazio.Core.Runner;

public interface IOperationRunner<T, R> 
    where T : OperationConfig 
    where R : OperationResult {
  Task<R> RunOperation(OperationStateAndConfig<T> op);
  R BuildErrorResult(OperationStateAndConfig<T> op, Exception ex);
}