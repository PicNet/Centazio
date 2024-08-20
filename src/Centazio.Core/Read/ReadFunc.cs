namespace Centazio.Core.Func;

public interface IReadOperationRunner {
  Task<ReadOperationResults> Run(DateTime start, ReadOperationStateAndConfig op);
}

public interface IReadOperationsFilterAndPrioritiser {
  ReadOperationStateAndConfig[] Prioritise(ReadOperationStateAndConfig[] ops);
}

public class DefaultReadOperationsFilterAndPrioritiser : IReadOperationsFilterAndPrioritiser {

  public ReadOperationStateAndConfig[] Prioritise(ReadOperationStateAndConfig[] ops) => ops;

}