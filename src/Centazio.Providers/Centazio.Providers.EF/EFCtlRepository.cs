using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Core.Types;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.EF;

public class EFCtlRepository(Func<AbstractCtlRepositoryDbContext> getdb) : AbstractCtlRepository {
  
  protected readonly Func<AbstractCtlRepositoryDbContext> getdb = getdb;

  public override Task<ICtlRepository> Initialise() => Task.FromResult<ICtlRepository>(this);
  public override ValueTask DisposeAsync() => ValueTask.CompletedTask;

  public override async Task<SystemState?> GetSystemState(SystemName system, LifecycleStage stage) {
    await using var db = getdb();
    return (await db.SystemStates.SingleOrDefaultAsync(s => s.System == system.Value && s.Stage == stage.Value))?.ToBase();
  }

  public override async Task<SystemState> SaveSystemState(SystemState state) {
    await using var db = getdb();
    var count = await db.ToDtoAttachAndUpdate<SystemState, SystemState.Dto>([state]);
    return count == 0 ? throw new Exception("SaveSystemState failed") : state;
  }

  public override async Task<SystemState> CreateSystemState(SystemName system, LifecycleStage stage) {
    var created = SystemState.Create(system, stage);
    
    await using var db = getdb();
    db.SystemStates.Add(DtoHelpers.ToDto<SystemState, SystemState.Dto>(created));
    var count = await db.SaveChangesAsync();
    if (count != 1) throw new Exception($"Error creating SystemState");
    return created;
  }

  public override async Task<ObjectState?> GetObjectState(SystemState system, ObjectName obj) {
    await using var db = getdb();
    return (await db.ObjectStates.SingleOrDefaultAsync(o => o.System == system.System.Value && o.Stage == system.Stage.Value && o.Object == obj.Value))?.ToBase();
  }

  public override async Task<ObjectState> SaveObjectState(ObjectState state) {
    await using var db = getdb();
    var count = await db.ToDtoAttachAndUpdate<ObjectState, ObjectState.Dto>([state]);
    return count == 0 ? throw new Exception("SaveObjectState failed") : state;
  }

  public override async Task<ObjectState> CreateObjectState(SystemState system, ObjectName obj, DateTime nextcheckpoint) {
    var created = ObjectState.Create(system.System, system.Stage, obj, nextcheckpoint);
    
    await using var db = getdb();
    db.ObjectStates.Add(DtoHelpers.ToDto<ObjectState, ObjectState.Dto>(created));
    var count = await db.SaveChangesAsync();
    if (count != 1) throw new Exception($"Error creating ObjectState");
    return created;                                     
  }
  
  protected override async Task<List<Map.Created>> CreateMapImpl(SystemName system, CoreEntityTypeName coretype, List<Map.Created> tocreate) {
    await using var db = getdb();
    var count = await db.ToDtoAttachAndCreate<Map.CoreToSysMap, Map.CoreToSysMap.Dto>(tocreate);
    if (count != tocreate.Count) throw new Exception();
    return tocreate;
  }

  protected override async Task<List<Map.Updated>> UpdateMapImpl(SystemName system, CoreEntityTypeName coretype, List<Map.Updated> toupdate) {
    await using var db = getdb();
    await db.ToDtoAttachAndUpdate<Map.CoreToSysMap, Map.CoreToSysMap.Dto>(toupdate);
    return toupdate;
  }

  protected override async Task<List<Map.CoreToSysMap>> GetExistingMapsByIds<V>(SystemName system, CoreEntityTypeName coretype, List<V> ids) {
    var idvals = ids.Select(id => id.Value);
    
    await using var db = getdb();
    var query = typeof(V) == typeof(SystemEntityId) 
        ? db.CoreToSystemMaps.Where(m => m.System == system.Value && m.CoreEntityTypeName == coretype.Value && idvals.Contains(m.SystemId)) 
        : db.CoreToSystemMaps.Where(m => m.System == system.Value && m.CoreEntityTypeName == coretype.Value && idvals.Contains(m.CoreId));
    return (await query.ToListAsync()).Select(dto => dto.ToBase()).ToList();
  }
}