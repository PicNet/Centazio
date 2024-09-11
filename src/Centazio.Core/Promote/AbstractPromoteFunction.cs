﻿using Centazio.Core.CoreRepo;
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

public record PromoteOperationResult<C>(
    IEnumerable<(StagedEntity Staged, C Core)> ToPromote, 
    IEnumerable<(StagedEntity Entity, ValidString Reason)> ToIgnore,
    EOperationResult Result, 
    string Message, 
    EResultType ResultType, 
    int ResultLength, 
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue,
    Exception? Exception = null) : OperationResult(Result, Message, ResultType, ResultLength, AbortVote, Exception)
        where C : ICoreEntity;

public record ErrorPromoteOperationResult<C>(string Message, EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) 
        : PromoteOperationResult<C>([], [], EOperationResult.Error, Message, EResultType.Error, 0, AbortVote, Exception) where C : ICoreEntity;