namespace Centazio.Core.Func;

public interface IReadOperationsFilterAndPrioritiser {
  IEnumerable<ReadOperationStateAndConfig> Prioritise(IEnumerable<ReadOperationStateAndConfig> ops);
}

public class DefaultReadOperationsFilterAndPrioritiser : IReadOperationsFilterAndPrioritiser {
  public IEnumerable<ReadOperationStateAndConfig> Prioritise(IEnumerable<ReadOperationStateAndConfig> ops) => ops;

}