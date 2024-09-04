using Centazio.Core.Ctl.Entities;

namespace centazio.core.Runner;

public interface IFunction {

  Task<IEnumerable<OperationResult>> Run(SystemState state, DateTime start);

}