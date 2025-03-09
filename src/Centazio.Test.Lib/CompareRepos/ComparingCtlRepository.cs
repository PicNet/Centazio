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
    var result1 = await (Task<List<Map.Created>>) repo1.GetType().GetMethod(nameof(CreateMapImpl), BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(repo1, [system, coretype, tocreate.Select(m => m with {}).ToList()])!;
    var result2 = await (Task<List<Map.Created>>) repo2.GetType().GetMethod(nameof(CreateMapImpl), BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(repo2, [system, coretype, tocreate.Select(m => m with {}).ToList()])!;
    return ValidateAndReturn(result1, result2);
  }

  protected override async Task<List<Map.Updated>> UpdateMapImpl(SystemName system, CoreEntityTypeName coretype, List<Map.Updated> toupdate) {
    var result1 = await (Task<List<Map.Updated>>) repo1.GetType().GetMethod(nameof(UpdateMapImpl), BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(repo1, [system, coretype, toupdate.Select(m => m with {}).ToList()])!;
    var result2 = await (Task<List<Map.Updated>>) repo2.GetType().GetMethod(nameof(UpdateMapImpl), BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(repo2, [system, coretype, toupdate.Select(m => m with {}).ToList()])!;
    return ValidateAndReturn(result1, result2);
  }

  protected override async Task<List<Map.CoreToSysMap>> GetExistingMapsByIds<V>(SystemName system, CoreEntityTypeName coretype, List<V> ids) {
    var result1 = await (Task<List<Map.CoreToSysMap>>) repo1.GetType().GetMethod(nameof(GetExistingMapsByIds), BindingFlags.Instance | BindingFlags.NonPublic)!.MakeGenericMethod(typeof(V)).Invoke(repo1, [system, coretype, ids])!;
    var result2 = await (Task<List<Map.CoreToSysMap>>) repo2.GetType().GetMethod(nameof(GetExistingMapsByIds), BindingFlags.Instance | BindingFlags.NonPublic)!.MakeGenericMethod(typeof(V)).Invoke(repo2, [system, coretype, ids])!;
    return ValidateAndReturn(result1, result2);
  }

  public override Task<ICtlRepository> Initialise() => Task.FromResult<ICtlRepository>(this);

  public override async ValueTask DisposeAsync() {
    await repo1.DisposeAsync();
    await repo2.DisposeAsync();
  }
  
  private T ValidateAndReturn<T>(T a, T b) {
    Json.ValidateJsonEqual(a, b, repo1.GetType().Name, repo2.GetType().Name);
    return a;
  }
  
}

