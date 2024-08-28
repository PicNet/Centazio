namespace Centazio.Core.Func;

public interface IReadOperationRunner {
  Task<ReadOperationResult> RunOperation(DateTime start, ReadOperationStateAndConfig op);
}