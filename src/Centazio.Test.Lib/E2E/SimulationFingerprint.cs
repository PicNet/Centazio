using System.Security.Cryptography;
using System.Text;
using Centazio.Core.Promote;
using Centazio.Core.Runner;

namespace Centazio.Test.Lib.E2E;

/// <summary>
/// Reduces what a simulation produced to a chain of hashes (one per epoch) so two runs can be compared.  Function
/// results are projected to plain snapshot records first: the result types hold value tuples and entities which the
/// project's json options do not serialise in full.
/// </summary>
public static class SimulationFingerprint {
  
  public static string Next(string previous, object state) => 
      Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(previous + Json.Serialize(state))));
  
  public static RunSnapshot Snapshot(Type function, FunctionRunResults results) => 
      new(function.Name, results.GetType().Name, results.OpResults.Select(ToOpSnapshot).ToList());

  private static OpSnapshot ToOpSnapshot(OpResultAndObject op) {
    var (promoted, ignored) = op.Result is SuccessPromoteOperationResult promote 
        ? (promote.ToPromote.Select(p => new PromotedSnapshot(p.StagedEntity.Id, p.CoreEntity.CoreId.Value)).ToList(), 
            promote.ToIgnore.Select(i => new IgnoredSnapshot(i.StagedEntity.Id, i.IgnoreReason.Value)).ToList()) 
        : ([], []);
    return new OpSnapshot(op.Object.Value, op.Result.Result.ToString(), op.Result.Message, op.Result.ChangedCount, promoted, ignored);
  }
}

public record RunSnapshot(string Function, string Outcome, List<OpSnapshot> Operations);
public record OpSnapshot(string Object, string Result, string Message, int ChangedCount, List<PromotedSnapshot> Promoted, List<IgnoredSnapshot> Ignored);
public record PromotedSnapshot(Guid StagedEntityId, string CoreId);
public record IgnoredSnapshot(Guid StagedEntityId, string IgnoreReason);

/// <summary>
/// Keeps the results of every function run, including those started by a changes notifier
/// </summary>
public class RecordingFunctionRunner(IFunctionRunner inner) : IFunctionRunner {
  
  private readonly List<RunSnapshot> runs = [];
  
  public async Task<FunctionRunResults> RunFunction(IRunnableFunction func, List<FunctionTrigger> triggers) {
    var results = await inner.RunFunction(func, triggers);
    runs.Add(SimulationFingerprint.Snapshot(func.GetType(), results));
    return results;
  }
  
  public List<RunSnapshot> TakeRuns() {
    var taken = runs.ToList();
    runs.Clear();
    return taken;
  }
}
