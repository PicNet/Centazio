using centazio.core.Ctl;
using centazio.core.Ctl.Entities;
using Centazio.Core.Helpers;
using Centazio.Core.Runner;

namespace Centazio.Core.Func;

public class ReadFunctionBase(
    ReadFunctionConfig cfg,
    ICtlRepository ctl,
    IReadOperationRunner runner,
    IReadOperationsFilterAndPrioritiser? prioritiser = null) : IFunction {
  
  private IReadOperationsFilterAndPrioritiser Prioritiser { get; } = prioritiser ?? new DefaultReadOperationsFilterAndPrioritiser();
  
  public async Task<IEnumerable<BaseFunctionOperationResult>> Run(SystemState state, DateTime start) => 
      await (await cfg.Operations
          .LoadOperationsStates(state, ctl))
          .GetReadyOperations(UtcDate.UtcNow)
          .Prioritise(Prioritiser)
          .RunOperationsTillAbort(runner, start);
}

internal static class ReadFunctionBaseHelperExtensions {
  internal static async Task<List<ReadOperationStateAndConfig>> LoadOperationsStates(this ICollection<ReadOperationConfig> ops, SystemState system, ICtlRepository ctl) {
    return (await Task.WhenAll(ops.Select(async op => new ReadOperationStateAndConfig(await ctl.GetOrCreateObjectState(system, op.Object), op))))
        .Where(op => op.State.Active)
        .ToList();
  }

  internal static IEnumerable<ReadOperationStateAndConfig> GetReadyOperations(this IEnumerable<ReadOperationStateAndConfig> states, DateTime now) {
    bool IsOperationReady(ReadOperationStateAndConfig op) {
      var next = op.Settings.Cron.Value.GetNextOccurrence(op.State.LastCompleted ?? DateTime.UtcNow.AddYears(-10));
      return next <= now;
    }
    return states.Where(IsOperationReady);
  }
  
  internal static IEnumerable<ReadOperationStateAndConfig> Prioritise(this IEnumerable<ReadOperationStateAndConfig> states, IReadOperationsFilterAndPrioritiser prioritiser) 
      => prioritiser.Prioritise(states); 
  
  internal static async Task<IEnumerable<ReadOperationResult>> RunOperationsTillAbort(this IEnumerable<ReadOperationStateAndConfig> ops, IReadOperationRunner runner, DateTime start) 
      => await ops
          .Select(op => runner.RunOperation(start, op))
          .Synchronous(r => r.AbortVote == EOperationAbortVote.Abort);
}