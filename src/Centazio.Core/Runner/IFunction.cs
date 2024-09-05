namespace Centazio.Core.Runner;

public interface IFunction {

  Task<IEnumerable<OperationResult>> Run(DateTime start);

}