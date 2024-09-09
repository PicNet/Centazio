using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Promote;

public abstract class AbstractPromoteFunction(IOperationsFilterAndPrioritiser<PromoteOperationConfig>? prioritiser = null) 
    : AbstractFunction<PromoteOperationConfig, PromoteOperationResult>(prioritiser);

public record PromoteOperationConfig(
    ObjectName Object, 
    ValidCron Cron, 
    DateTime FirstTimeCheckpoint, 
    Func<OperationStateAndConfig<PromoteOperationConfig>, IEnumerable<StagedEntity>, Task<PromoteOperationResult>> EvaluateEntitiesToPromote,
    Func<OperationStateAndConfig<PromoteOperationConfig>, IEnumerable<ICoreEntity>, Task> PromoteEntities) : OperationConfig(Object, Cron, FirstTimeCheckpoint);

public record PromoteOperationResult(
    IEnumerable<(StagedEntity Staged, ICoreEntity Core)> ToPromote, 
    IEnumerable<(StagedEntity Entity, ValidString Reason)> ToIgnore,
    EOperationResult Result, 
    string Message, 
    EResultType ResultType, 
    int ResultLength, 
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue,
    Exception? Exception = null) : OperationResult(Result, Message, ResultType, ResultLength, AbortVote, Exception);

public record ErrorPromoteOperationResult(string Message, EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) 
        : PromoteOperationResult([], [], EOperationResult.Error, Message, EResultType.Error, 0, AbortVote, Exception);