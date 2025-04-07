using Centazio.Core.Stage;

namespace Centazio.Core.Promote;

internal record SuccessPromoteOperationResult(
    List<(StagedEntity StagedEntity, ISystemEntity SystemEntity, ICoreEntity CoreEntity)> ToPromote,
    List<(StagedEntity StagedEntity, ValidString IgnoreReason)> ToIgnore,
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue)
    : OperationResult(EOperationResult.Success,
        $"SuccessPromoteOperationResult Promote[{ToPromote.Count}] Ignore[{ToIgnore.Count}]",
        ToPromote.Count,
        AbortVote);
