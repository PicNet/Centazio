namespace Centazio.Core.Runner;

public interface IOperationRunner<C> where C : OperationConfig {
  Task<OperationResult> RunOperation(OperationStateAndConfig<C> op);
  OperationResult BuildErrorResult(OperationStateAndConfig<C> op, Exception ex);
}