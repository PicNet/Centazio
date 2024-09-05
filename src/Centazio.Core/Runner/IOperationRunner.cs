namespace Centazio.Core.Runner;

public interface IOperationRunner<T> where T : OperationConfig {
  Task<OperationResult> RunOperation(DateTime funcstart, OperationStateAndConfig<T> op);
}