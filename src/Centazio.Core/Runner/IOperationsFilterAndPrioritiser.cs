namespace centazio.core.Runner;

public interface IOperationsFilterAndPrioritiser {
  IEnumerable<OperationStateAndConfig> Prioritise(IEnumerable<OperationStateAndConfig> ops);
}

public class DefaultOperationsFilterAndPrioritiser : IOperationsFilterAndPrioritiser {
  public IEnumerable<OperationStateAndConfig> Prioritise(IEnumerable<OperationStateAndConfig> ops) => ops;

}