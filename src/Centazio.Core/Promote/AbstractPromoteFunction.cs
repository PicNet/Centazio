using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Promote;

public abstract class AbstractPromoteFunction<C>(IOperationsFilterAndPrioritiser<PromoteOperationConfig<C>>? prioritiser = null) 
    : AbstractFunction<PromoteOperationConfig<C>, PromoteOperationResult<C>>(prioritiser) where C : ICoreEntity;

public record PromoteOperationConfig<C>(
    ObjectName Object, 
    ValidCron Cron, 
    DateTime FirstTimeCheckpoint, 
    Func<OperationStateAndConfig<PromoteOperationConfig<C>>, IEnumerable<StagedEntity>, Task<PromoteOperationResult<C>>> EvaluateEntitiesToPromote) : OperationConfig(Object, Cron, FirstTimeCheckpoint) where C : ICoreEntity;

public record PromoteOperationResult<C> : OperationResult where C : ICoreEntity {
    
    public IEnumerable<(StagedEntity Staged, C Core)> ToPromote { get; }
    public IEnumerable<(StagedEntity Entity, ValidString Reason)> ToIgnore { get; }
    
    protected PromoteOperationResult(
        IEnumerable<(StagedEntity Staged, C Core)> topromote, 
        IEnumerable<(StagedEntity Entity, ValidString Reason)> toignore,
        EOperationResult result, 
        string message, 
        EOperationAbortVote abort = EOperationAbortVote.Continue,
        Exception? exception = null) : base(result, message, abort, exception) {
      ToPromote = topromote;
      ToIgnore = toignore;
    }
}

public record SuccessPromoteOperationResult<C>(
    IEnumerable<(StagedEntity Staged, C Core)> ToPromote, 
    IEnumerable<(StagedEntity Entity, ValidString Reason)> ToIgnore, 
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue) : PromoteOperationResult<C>(ToPromote, ToIgnore, EOperationResult.Success, "", AbortVote) where C : ICoreEntity;

public record ErrorPromoteOperationResult<C>(EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) 
        : PromoteOperationResult<C>(Array.Empty<(StagedEntity Staged, C Core)>(), 
                Array.Empty<(StagedEntity Entity, ValidString Reason)>(), EOperationResult.Error, "", AbortVote, Exception) where C : ICoreEntity;