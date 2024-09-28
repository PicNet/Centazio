﻿using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Centazio.Providers.SqlServer.Ctl;

public class SqlServerCtlRepository(Func<SqlConnection> newconn) : ICtlRepository {

  internal static readonly string SCHEMA = nameof(Core.Ctl).ToLower();
  internal const string SYSTEM_STATE_TBL = nameof(SystemState);
  internal const string OBJECT_STATE_TBL = "ObjectState";

  public async Task<SqlServerCtlRepository> Initalise() {
    await using var conn = newconn();
    await conn.ExecuteAsync($@"
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'{SCHEMA}')
  EXEC('CREATE SCHEMA [{SCHEMA}] AUTHORIZATION [dbo]');

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{SYSTEM_STATE_TBL}' AND xtype='U')
BEGIN
  CREATE TABLE {SCHEMA}.{SYSTEM_STATE_TBL} (
    System nvarchar (32) NOT NULL,
    Stage nvarchar (32) NOT NULL,
    Active bit NOT NULL, 
    Status tinyint NOT NULL,
    DateCreated datetime2 NOT NULL,    
    DateUpdated datetime2 NULL,
    LastStarted datetime2 NULL,
    LastCompleted datetime2 NULL,
    PRIMARY KEY (System, Stage)
  )
END

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{OBJECT_STATE_TBL}' AND xtype='U')
BEGIN
  CREATE TABLE {SCHEMA}.{OBJECT_STATE_TBL} (
    System nvarchar (32) NOT NULL,
    Stage nvarchar (32) NOT NULL,
    Active bit NOT NULL, 
    Object nvarchar (32) NOT NULL,
    DateCreated datetime2 NOT NULL,
    DateUpdated datetime2 NULL,
    LastResult tinyint NOT NULL,
    LastAbortVote tinyint NOT NULL,

    LastStart datetime2 NULL,
    LastCompleted datetime2 NULL,
    LastSuccessStart datetime2 NULL,
    LastSuccessCompleted datetime2 NULL,
    LastRunMessage nvarchar(256) NULL,
    LastRunException nvarchar(max) NULL,

    PRIMARY KEY (System, Stage, Object),
    FOREIGN KEY  (System, Stage) REFERENCES {SCHEMA}.{SYSTEM_STATE_TBL} (System, Stage)  
  )
END
");
    return this;
  }
  
  public async Task<SystemState?> GetSystemState(SystemName system, LifecycleStage stage) {
    await using var conn = newconn();
    var raw = await conn.QuerySingleOrDefaultAsync<SystemState.Dto>($"SELECT * FROM {SCHEMA}.{SYSTEM_STATE_TBL} WHERE System=@System AND Stage=@Stage", new { System=system, Stage=stage });
    return raw is null ? null : (SystemState) raw;
  }

  public async Task<SystemState> SaveSystemState(SystemState state) {
    await using var conn = newconn();
    var count = await conn.ExecuteAsync($@"
UPDATE {SCHEMA}.{SYSTEM_STATE_TBL} 
SET Active=@Active, Status=@Status, DateUpdated=@DateUpdated, LastStarted=@LastStarted, LastCompleted=@LastCompleted
WHERE System=@System AND Stage=@Stage", state);
    return count == 0 ? throw new Exception("SaveSystemState failed") : state;
  }

  public IObjectStateRepo<O> GetObjectStateRepo<O>() where O : ObjectName {
    return new SqlObjectStateRepository<O>(newconn);
  }

  public async Task<SystemState> CreateSystemState(SystemName system, LifecycleStage stage) {
    await using var conn = newconn();
    var created = SystemState.Create(system, stage);
    await conn.ExecuteAsync($@"
INSERT INTO {SCHEMA}.{SYSTEM_STATE_TBL} 
(System, Stage, Active, Status, DateCreated)
VALUES (@System, @Stage, @Active, @Status, @DateCreated)", created);
    
    return created;
  }

  public virtual ValueTask DisposeAsync() { return ValueTask.CompletedTask; }
  
  class SqlObjectStateRepository<O>(Func<SqlConnection> newconn) : IObjectStateRepo<O> where O : ObjectName {
    
    public async Task<ObjectState<O>?> GetObjectState(SystemState system, O obj) {
      await using var conn = newconn();
      var raw = await conn.QuerySingleOrDefaultAsync<ObjectState<O>.Dto>(@$"
  SELECT * FROM {SCHEMA}.{OBJECT_STATE_TBL} 
  WHERE System=@System AND Stage=@Stage AND Object=@Object", new { system.System, system.Stage, Object=obj });
      return raw?.ToObjectState();
    }

    public async Task<ObjectState<O>> SaveObjectState(ObjectState<O> state) {
      await using var conn = newconn();
      var count = await conn.ExecuteAsync($@"
  UPDATE {SCHEMA}.{OBJECT_STATE_TBL} 
  SET 
    Active=@Active, 
    LastResult=@LastResult, 
    LastAbortVote=@LastAbortVote, 
    DateUpdated=@DateUpdated, 
    LastStart=@LastStart,  
    LastCompleted=@LastCompleted,
    LastSuccessStart=@LastSuccessStart,
    LastSuccessCompleted=@LastSuccessCompleted,
    LastRunMessage=@LastRunMessage, 
    LastRunException=@LastRunException 
  WHERE System=@System AND Stage=@Stage AND Object=@Object", state);
      return count == 0 ? throw new Exception("SaveObjectState failed") : state;
    }
    
    public async Task<ObjectState<O>> CreateObjectState(SystemState system, O obj) {
      await using var conn = newconn();
      
      var created = ObjectState<O>.Create(system.System, system.Stage, obj);
      await conn.ExecuteAsync($@"
  INSERT INTO {SCHEMA}.{OBJECT_STATE_TBL}
  (System, Stage, Object, Active, DateCreated, LastResult, LastAbortVote)
  VALUES (@System, @System, @Object, @Active, @DateCreated, @LastResult, @LastAbortVote)
  ", created);
      
      return created;
    }
    
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

  }
}

