using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Promote;

public record StagedAndCoreEntity(StagedEntity Staged, ICoreEntity Core);
public record StagedEntityAndIgnoreReason(StagedEntity Staged, ValidString Reason);

public abstract record PromoteOperationResult(
    List<StagedAndCoreEntity> ToPromote,
    List<StagedEntityAndIgnoreReason> ToIgnore,
    EOperationResult Result,
    string Message,
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue,
    Exception? Exception = null)
    : OperationResult(Result, Message, AbortVote, Exception), ILoggable {

    public object LoggableValue => $"ToPromote[{ToPromote.Count}] ToIgnore[{ToIgnore.Count}] Result[{Result}] Message[{Message}]";
}

public record SuccessPromoteOperationResult(
    List<StagedAndCoreEntity> ToPromote,
    List<StagedEntityAndIgnoreReason> ToIgnore,
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue)
    : PromoteOperationResult(ToPromote,
        ToIgnore,
        EOperationResult.Success,
        $"SuccessPromoteOperationResult Promote[{ToPromote.Count}] Ignore[{ToIgnore.Count}]",
        AbortVote);

public record ErrorPromoteOperationResult(EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null)
    : PromoteOperationResult([],
        [],
        EOperationResult.Error,
        $"ErrorPromoteOperationResult[{Exception?.Message ?? "na"}] - AbortVote[{AbortVote}]",
        AbortVote,
        Exception);