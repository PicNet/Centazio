namespace Centazio.Core.Runner;

public interface IOperationRunner<C, O, R> 
    where C : OperationConfig<O>
    where O : ObjectName
    where R : OperationResult {
  Task<R> RunOperation(OperationStateAndConfig<C, O> op);
  R BuildErrorResult(OperationStateAndConfig<C, O> op, Exception ex);
}