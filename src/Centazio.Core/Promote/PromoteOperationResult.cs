using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Centazio.Core.Stage;

namespace Centazio.Core.Promote;

public abstract record PromoteOperationResult(
    List<(StagedEntity StagedEntity, ISystemEntity SystemEntity, ICoreEntity CoreEntity)> ToPromote,
    List<(StagedEntity StagedEntity, ValidString IgnoreReason)> ToIgnore,
    EOperationResult Result,
    string Message,
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue,
    Exception? Exception = null)
    : OperationResult(Result, Message, AbortVote, null, Exception), ILoggable {

    public string LoggableValue => $"ToPromote[{ToPromote.Count}] ToIgnore[{ToIgnore.Count}] Result[{Result}] Message[{Message}]";
}

internal sealed record SuccessPromoteOperationResult(
    List<(StagedEntity StagedEntity, ISystemEntity SystemEntity, ICoreEntity CoreEntity)> ToPromote,
    List<(StagedEntity StagedEntity, ValidString IgnoreReason)> ToIgnore,
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue)
    : PromoteOperationResult(ToPromote,
        ToIgnore,
        EOperationResult.Success,
        $"SuccessPromoteOperationResult Promote[{ToPromote.Count}] Ignore[{ToIgnore.Count}]",
        AbortVote);

internal sealed record ErrorPromoteOperationResult(EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null)
    : PromoteOperationResult([],
        [],
        EOperationResult.Error,
        $"ErrorPromoteOperationResult[{Exception?.Message ?? "na"}] - AbortVote[{AbortVote}]",
        AbortVote,
        Exception);