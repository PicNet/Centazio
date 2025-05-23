using System.Reflection;

namespace Centazio.Test.Lib.CompareRepos;

public class ComparingCtlRepository(AbstractCtlRepository repo1, AbstractCtlRepository repo2) : AbstractCtlRepository {

  
  public override async Task<SystemState?> GetSystemState(SystemName system, LifecycleStage stage) {
    var (result1, result2) = (await repo1.GetSystemState(system, stage), await repo2.GetSystemState(system, stage));
    return ValidateAndReturn(result1, result2);
  }

  public override async Task<SystemState> CreateSystemState(SystemName system, LifecycleStage stage) {
    var (result1, result2) = (await repo1.CreateSystemState(system, stage), await repo2.CreateSystemState(system, stage));
    return ValidateAndReturn(result1, result2);
  }

  public override async Task<SystemState> SaveSystemState(SystemState state) {
    var (result1, result2) = (await repo1.SaveSystemState(state), await repo2.SaveSystemState(state));
    return ValidateAndReturn(result1, result2);
  }

  public override async Task<ObjectState?> GetObjectState(SystemState system, ObjectName obj) {
    var (result1, result2) = (await repo1.GetObjectState(system, obj), await repo2.GetObjectState(system, obj));
    return ValidateAndReturn(result1, result2);
  }

  public override async Task<ObjectState> CreateObjectState(SystemState system, ObjectName obj, DateTime nextcheckpoint) {
    var (result1, result2) = (await repo1.CreateObjectState(system, obj, nextcheckpoint), await repo2.CreateObjectState(system, obj, nextcheckpoint));
    return ValidateAndReturn(result1, result2);
  }

  public override async Task<ObjectState> SaveObjectState(ObjectState state) {
    var (result1, result2) = (await repo1.SaveObjectState(state), await repo2.SaveObjectState(state));
    return ValidateAndReturn(result1, result2);
  }

  protected override async Task<List<Map.Created>> CreateMapImpl(SystemName system, CoreEntityTypeName coretype, List<Map.Created> tocreate) {
    var result1 = await Invoke<List<Map.Created>>(repo1, nameof(CreateMapImpl), [system, coretype, tocreate.Select(m => m with {}).ToList()])!;
    var result2 = await Invoke<List<Map.Created>>(repo2, nameof(CreateMapImpl), [system, coretype, tocreate.Select(m => m with {}).ToList()])!;
    return ValidateAndReturn(result1, result2);
  }

  protected override async Task<List<Map.Updated>> UpdateMapImpl(SystemName system, CoreEntityTypeName coretype, List<Map.Updated> toupdate) {
    var result1 = await Invoke<List<Map.Updated>>(repo1, nameof(UpdateMapImpl), [system, coretype, toupdate.Select(m => m with {}).ToList()]);
    var result2 = await Invoke<List<Map.Updated>>(repo2, nameof(UpdateMapImpl), [system, coretype, toupdate.Select(m => m with {}).ToList()]);
    return ValidateAndReturn(result1, result2);
  }

  protected override async Task<List<EntityChange>> SaveEntityChangesImpl(List<EntityChange> batch) {
    var result1 = await Invoke<List<EntityChange>>(repo1, nameof(SaveEntityChangesImpl), [batch])!;
    var result2 = await Invoke<List<EntityChange>>(repo2, nameof(SaveEntityChangesImpl), [batch])!;
    return ValidateAndReturn(result1, result2);
  }

  protected override async Task<List<Map.CoreToSysMap>> GetExistingMapsByIds<V>(SystemName system, CoreEntityTypeName coretype, List<V> ids) {
    var result1 = await Invoke<List<Map.CoreToSysMap>>(repo1, nameof(GetExistingMapsByIds), [system, coretype, ids], typeof(V));
    var result2 = await Invoke<List<Map.CoreToSysMap>>(repo2, nameof(GetExistingMapsByIds), [system, coretype, ids], typeof(V));
    return ValidateAndReturn(result1, result2);
  }

  public override Task<ICtlRepository> Initialise() => Task.FromResult<ICtlRepository>(this);

  public override async ValueTask DisposeAsync() {
    await repo1.DisposeAsync();
    await repo2.DisposeAsync();
  }
  
  private async Task<T> Invoke<T>(AbstractCtlRepository repo, string method, object[] args, Type? generictype = null) {
    var m = repo.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new Exception();
    if (generictype is not null) m = m.MakeGenericMethod(generictype);
    return await (Task<T>) (m.Invoke(repo1, args) ?? throw new Exception());
  }
  
  private List<T> ValidateAndReturn<T>(List<T> a, List<T> b) {
    Json.ValidateJsonEqual(a.Cast<object>(), b.Cast<object>(), repo1.GetType().Name, repo2.GetType().Name);
    return a;
  }
  
  private T ValidateAndReturn<T>(T a, T b) {
    Json.ValidateJsonEqual(a, b, repo1.GetType().Name, repo2.GetType().Name);
    return a;
  }
  
}

