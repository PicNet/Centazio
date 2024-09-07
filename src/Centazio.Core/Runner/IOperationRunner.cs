﻿namespace Centazio.Core.Runner;

public interface IOperationRunner<T, R> 
    where T : OperationConfig 
    where R : OperationResult {
  Task<R> RunOperation(DateTime funcstart, OperationStateAndConfig<T> op);
}