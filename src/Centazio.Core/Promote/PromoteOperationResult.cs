using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;
using Centazio.Core.Write;

namespace Centazio.Core.Promote;

public static class Containers {
}

public record StagedSysCoreCont(StagedEntity Staged, ISystemEntity SysEnt, ICoreEntity CoreEnt);
public record StagedSysCont(StagedEntity Staged, ISystemEntity SysEnt);
public record StagedCoreCont(StagedEntity Staged, ICoreEntity CoreEnt);
public record StagedIgnoreReasonCont(StagedEntity Staged, ValidString IgnoreReason);

public abstract record PromoteOperationResult(
    List<StagedSysCoreCont> ToPromote,
    List<StagedIgnoreReasonCont> ToIgnore,
    EOperationResult Result,
    string Message,
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue,
    Exception? Exception = null)
    : OperationResult(Result, Message, AbortVote, Exception), ILoggable {

    public object LoggableValue => $"ToPromote[{ToPromote.Count}] ToIgnore[{ToIgnore.Count}] Result[{Result}] Message[{Message}]";
}

public record SuccessPromoteOperationResult(
    List<StagedSysCoreCont> ToPromote,
    List<StagedIgnoreReasonCont> ToIgnore,
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