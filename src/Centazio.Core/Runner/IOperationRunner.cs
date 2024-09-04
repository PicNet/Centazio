namespace centazio.core.Runner;

public interface IOperationRunner {
  Task<OperationResult> RunOperation(DateTime start, OperationStateAndConfig op);
}