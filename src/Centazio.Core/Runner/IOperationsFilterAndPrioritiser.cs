namespace centazio.core.Runner;

public interface IOperationsFilterAndPrioritiser<T> where T : OperationConfig {
  IEnumerable<OperationStateAndConfig<T>> Prioritise(IEnumerable<OperationStateAndConfig<T>> ops);
}

public class DefaultOperationsFilterAndPrioritiser<T> : IOperationsFilterAndPrioritiser<T> where T : OperationConfig {
  public IEnumerable<OperationStateAndConfig<T>> Prioritise(IEnumerable<OperationStateAndConfig<T>> ops) => ops;

}