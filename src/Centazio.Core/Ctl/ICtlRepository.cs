namespace Centazio.Core.Ctl;

public interface ICtlRepository : IAsyncDisposable {

  Task<IDbTransactionWrapper> BeginTransaction(IDbTransactionWrapper? reuse = null);
  
  Task<ICtlRepository> Initialise();
  
  Task<SystemState?> GetSystemState(SystemName system, LifecycleStage stage);
  Task<SystemState> CreateSystemState(SystemName system, LifecycleStage stage);
  Task<SystemState> SaveSystemState(SystemState state);
  Task<SystemState> GetOrCreateSystemState(SystemName system, LifecycleStage stage);
  
  Task<ObjectState?> GetObjectState(SystemState system, ObjectName obj);
  Task<ObjectState> CreateObjectState(SystemState system, ObjectName obj, DateTime nextcheckpoint);
  Task<ObjectState> SaveObjectState(ObjectState state);
  Task<ObjectState> GetOrCreateObjectState(SystemState system, ObjectName obj, DateTime firstcheckpoint);
  
  Task<List<Map.Created>> CreateSysMap(SystemName system, CoreEntityTypeName coretype, List<Map.Created> tocreate);
  Task<List<Map.Updated>> UpdateSysMap(SystemName system, CoreEntityTypeName coretype, List<Map.Updated> toupdate);
  
  Task<List<EntityChange>> SaveEntityChanges(List<EntityChange> changes);
  Task<List<EntityChange>> GetEntityChanges(CoreEntityTypeName coretype, DateTime after);
  Task<List<EntityChange>> GetEntityChanges(SystemName system, SystemEntityTypeName systype, DateTime after);
  
  Task<(List<CoreAndPendingCreateMap> Created, List<CoreAndPendingUpdateMap> Updated)> GetNewAndExistingMapsFromCores(SystemName system, CoreEntityTypeName coretype, List<ICoreEntity> coreents);
  Task<List<Map.CoreToSysMap>> GetMapsFromSystemIds(SystemName system, CoreEntityTypeName coretype, List<SystemEntityId> systemids);
  
  Task<Dictionary<CoreEntityId, SystemEntityId>> GetRelatedSystemIdsFromCores(SystemName system, CoreEntityTypeName coretype, List<ICoreEntity> coreents, string foreignkey);
  Task<Dictionary<SystemEntityId, CoreEntityId>> GetRelatedCoreIdsFromSystemIds(SystemName system, CoreEntityTypeName coretype, List<ISystemEntity> sysents, string foreignkey, bool mandatory);

}
