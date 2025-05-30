using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.EF;

public abstract class AbstractEFCtlRepository(Func<AbstractCtlRepositoryDbContext> getdb) : AbstractCtlRepository {
  
  private readonly EfTransactionManager<AbstractCtlRepositoryDbContext> mgr = new(getdb);
  
  public override Task<IDbTransactionWrapper> BeginTransaction(IDbTransactionWrapper? reuse = null) => mgr.BeginTransaction(reuse);
  protected Task<T> UseDb<T>(Func<AbstractCtlRepositoryDbContext, Task<T>> func) => mgr.UseDb(func);
  
  public override Task<ICtlRepository> Initialise() => Task.FromResult<ICtlRepository>(this);
  public override ValueTask DisposeAsync() => mgr.DisposeAsync();

  public override async Task<SystemState?> GetSystemState(SystemName system, LifecycleStage stage) => 
      await UseDb(async db => 
          (await db.SystemStates.SingleOrDefaultAsync(s => s.System == system.Value && s.Stage == stage.Value))?.ToBase());

  public override async Task<SystemState> SaveSystemState(SystemState state) => 
      await UseDb(async db => {
        var count = await db.ToDtoAttachAndUpdate<SystemState, SystemState.Dto>([state]);
        return count == 0 ? throw new Exception("SaveSystemState failed") : state;
      });

  public override async Task<SystemState> CreateSystemState(SystemName system, LifecycleStage stage) => 
      await UseDb(async db => {
        var created = SystemState.Create(system, stage);
        db.SystemStates.Add(DtoHelpers.ToDto<SystemState, SystemState.Dto>(created));
        var count = await db.SaveChangesAsync();
        if (count != 1) throw new Exception($"Error creating SystemState");
        return created;
      });

  public override async Task<ObjectState?> GetObjectState(SystemState system, ObjectName obj) => 
      await UseDb(async db => 
        (await db.ObjectStates.SingleOrDefaultAsync(o => o.System == system.System.Value && o.Stage == system.Stage.Value && o.Object == obj.Value))?.ToBase());

  public override async Task<ObjectState> SaveObjectState(ObjectState state) => 
      await UseDb(async db => {
        var count = await db.ToDtoAttachAndUpdate<ObjectState, ObjectState.Dto>([state]);
        return count == 0 ? throw new Exception("SaveObjectState failed") : state;
      });

  public override async Task<ObjectState> CreateObjectState(SystemState system, ObjectName obj, DateTime nextcheckpoint) => 
      await UseDb(async db => {
        var created = ObjectState.Create(system.System, system.Stage, obj, nextcheckpoint);
        db.ObjectStates.Add(DtoHelpers.ToDto<ObjectState, ObjectState.Dto>(created));
        var count = await db.SaveChangesAsync();
        if (count != 1) throw new Exception($"Error creating ObjectState");
        return created;
      });

  protected override async Task<List<Map.Created>> CreateMapImpl(SystemName system, CoreEntityTypeName coretype, List<Map.Created> tocreate) => 
      await UseDb(async db => {
        var count = await db.ToDtoAttachAndCreate<Map.CoreToSysMap, Map.CoreToSysMap.Dto>(tocreate);
        if (count != tocreate.Count) throw new Exception();
        return tocreate;
      });

  protected override async Task<List<Map.Updated>> UpdateMapImpl(SystemName system, CoreEntityTypeName coretype, List<Map.Updated> toupdate) => 
      await UseDb(async db => {
        await db.ToDtoAttachAndUpdate<Map.CoreToSysMap, Map.CoreToSysMap.Dto>(toupdate);
        return toupdate;
      });

  protected override async Task<List<EntityChange>> SaveEntityChangesImpl(List<EntityChange> changes) => 
      await UseDb(async db => {
        await db.ToDtoAttachAndCreate<EntityChange, EntityChange.Dto>(changes);
        return changes;
      });

  public override async Task<List<EntityChange>> GetEntityChanges(CoreEntityTypeName coretype, DateTime after) => 
      await UseDb(async db => (await db.EntityChanges
          .Where(c => c.CoreEntityTypeName == coretype && c.ChangeDate > after).ToListAsync())
      .Select(dto => dto.ToBase())
      .ToList());

  public override async Task<List<EntityChange>> GetEntityChanges(SystemName system, SystemEntityTypeName systype, DateTime after) => 
      await UseDb(async db => 
          (await db.EntityChanges.Where(c => c.System == system && c.SystemEntityTypeName == systype && c.ChangeDate > after).ToListAsync())
          .Select(dto => dto.ToBase())
          .ToList());

  protected override async Task<List<Map.CoreToSysMap>> GetExistingMapsByIds<V>(SystemName system, CoreEntityTypeName coretype, List<V> ids) {
    var issysent = typeof(V) == typeof(SystemEntityId);
    var idvals = ids.Select(id => id.Value);
    
    return await UseDb(async db => {
      var query = issysent 
          ? db.CoreToSystemMaps.Where(m => m.System == system.Value && m.CoreEntityTypeName == coretype.Value && idvals.Contains(m.SystemId)) 
          : db.CoreToSystemMaps.Where(m => m.System == system.Value && m.CoreEntityTypeName == coretype.Value && idvals.Contains(m.CoreId));
      return (await query.ToListAsync())
          .Select(dto => dto.ToBase())
          .OrderBy(e => issysent ? e.SystemId.Value : e.CoreId.Value)
          .ToList();
    });
  }
  
  protected async Task CreateSchema(IDbFieldsHelper dbf, AbstractCtlRepositoryDbContext db) {
    await db.ExecSql(dbf.GenerateCreateTableScript(db.Settings.SchemaName, db.Settings.SystemStateTableName, dbf.GetDbFields<SystemState>(), [nameof(SystemState.System), nameof(SystemState.Stage)]));
    await db.ExecSql(dbf.GenerateCreateTableScript(db.Settings.SchemaName, db.Settings.ObjectStateTableName, dbf.GetDbFields<ObjectState>(), [nameof(ObjectState.System), nameof(ObjectState.Stage), nameof(ObjectState.Object)],
        [],
        [new ForeignKey([nameof(SystemState.System), nameof(SystemState.Stage)], db.Settings.SchemaName, db.Settings.SystemStateTableName)]));
    await db.ExecSql(dbf.GenerateCreateTableScript(db.Settings.SchemaName, db.Settings.CoreToSysMapTableName, dbf.GetDbFields<Map.CoreToSysMap>(), 
        [nameof(Map.CoreToSysMap.System), nameof(Map.CoreToSysMap.CoreEntityTypeName), nameof(Map.CoreToSysMap.CoreId)],
        [[nameof(Map.CoreToSysMap.System), nameof(Map.CoreToSysMap.CoreEntityTypeName), nameof(Map.CoreToSysMap.SystemId)]]));
    await db.ExecSql(dbf.GenerateCreateTableScript(db.Settings.SchemaName, db.Settings.EntityChangeTableName, dbf.GetDbFields<EntityChange>(), [nameof(EntityChange.CoreEntityTypeName), nameof(EntityChange.CoreId), nameof(EntityChange.ChangeDate)]));
  }
  
  protected async Task DropTablesImpl(IDbFieldsHelper dbf, AbstractCtlRepositoryDbContext db) { 
    await db.ExecSql(dbf.GenerateDropTableScript(db.Settings.SchemaName, db.Settings.CoreToSysMapTableName));
    await db.ExecSql(dbf.GenerateDropTableScript(db.Settings.SchemaName, db.Settings.ObjectStateTableName));
    await db.ExecSql(dbf.GenerateDropTableScript(db.Settings.SchemaName, db.Settings.SystemStateTableName));
    await db.ExecSql(dbf.GenerateDropTableScript(db.Settings.SchemaName, db.Settings.EntityChangeTableName));
  }
}