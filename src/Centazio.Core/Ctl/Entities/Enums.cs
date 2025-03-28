﻿namespace Centazio.Core.Ctl.Entities;

public enum EOperationResult { Unknown, Success, Error }
public enum EOperationAbortVote { Unknown, Continue, Abort }
public enum ESystemStateStatus { Idle, Running }
public enum EEntityMappingStatus { 
  PendingCreate, 
  Error, 
  SuccessCreate,
  SuccessUpdate,
  Orphaned, // no longer has the 'Core' entitiy that this TSE was originaly created for
  MissingTarget // could not find the 'Target' entity in the target system to create link
}