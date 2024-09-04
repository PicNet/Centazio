namespace centazio.core.Runner;

public interface IOperationRunner<T> where T : OperationConfig {
  Task<OperationResult> RunOperation(DateTime start, OperationStateAndConfig<T> op);
}