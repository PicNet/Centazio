using centazio.core.Ctl.Entities;
using Centazio.Core.Func;

namespace Centazio.Core.Runner;

public interface IFunction {

  Task<IEnumerable<BaseFunctionOperationResult>> Run(SystemState state, DateTime start);

}