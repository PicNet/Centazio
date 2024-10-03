namespace Centazio.Core.Runner;

public interface IOperationRunner<C, R> 
    where C : OperationConfig
    where R : OperationResult {
  Task<R> RunOperation(OperationStateAndConfig<C> op);
  R BuildErrorResult(OperationStateAndConfig<C> op, Exception ex);
}