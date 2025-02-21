using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Core.Promote;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Core.Stage;
using Centazio.Core.Write;
using Centazio.Test.Lib.InMemRepos;

namespace Centazio.Test.Lib;

public static class TestingFactories {
  
  private static CentazioSettings? settings;
  public static CentazioSettings Settings(string? environment=null) => settings ??= Settings<CentazioSettings>(environment);
  public static E Settings<E>(string? environment=null) => new SettingsLoader().Load<E>(environment ?? CentazioConstants.DEFAULT_ENVIRONMENT);
  
  public static CentazioSecrets Secrets() => Secrets<CentazioSecrets>();
  public static E Secrets<E>() => new SecretsFileLoader(Settings().GetSecretsFolder()).Load<E>(CentazioConstants.DEFAULT_ENVIRONMENT);
  
  public static TestingStagedEntityRepository SeRepo() => new(); 
  public static TestingInMemoryBaseCtlRepository CtlRepo() => new();
  public static TestingInMemoryCoreStorageRepository CoreRepo() => new();
  public static IOperationRunner<ReadOperationConfig> ReadRunner(IStagedEntityRepository? serepo = null) => new ReadOperationRunner(serepo ?? SeRepo());
  public static IOperationRunner<PromoteOperationConfig> PromoteRunner(
      IStagedEntityRepository? serepo = null, 
      ICtlRepository? ctl = null, 
      ICoreStorage? core = null) => 
      new PromoteOperationRunner(serepo ?? SeRepo(), core ?? CoreRepo(), ctl ?? CtlRepo());

  public static CoreEntityAndMeta NewCoreEntity(string first, string last, CoreEntityId? id = null) {
    id ??= new(Guid.NewGuid().ToString());
    var dob = DateOnly.MinValue;
    var core = new CoreEntity(id, first, last, dob);
    return CoreEntityAndMeta.Create(Constants.System1Name, new (id.Value), core, Helpers.TestingCoreEntityChecksum(core));
  }

  public static WriteOperationRunner<C> WriteRunner<C>(TestingInMemoryBaseCtlRepository? ctl = null, TestingInMemoryCoreStorageRepository? core = null) where C : WriteOperationConfig  
      => new(ctl ?? CtlRepo(), core ?? CoreRepo());
  
  public static async Task<OperationStateAndConfig<ReadOperationConfig>> CreateReadOpStateAndConf(ICtlRepository repo) {
    var success = EOperationResult.Success.ToString();
    return new OperationStateAndConfig<ReadOperationConfig>(await repo.CreateObjectState(await repo.CreateSystemState(new(success), new(success)), new SystemEntityTypeName(success), UtcDate.UtcNow),
        
        new BaseFunctionConfig(),
        new(new SystemEntityTypeName(success), TestingDefaults.CRON_EVERY_SECOND, GetEmptyResult),
        DateTime.MinValue);
  }

  public static async Task<OperationStateAndConfig<ReadOperationConfig>> CreateErroringOpStateAndConf(ICtlRepository repo) {
    var error = EOperationResult.Error.ToString();
    return new OperationStateAndConfig<ReadOperationConfig>(await repo.CreateObjectState(await repo.CreateSystemState(new(error), new(error)), new SystemEntityTypeName(error), UtcDate.UtcNow),
        new BaseFunctionConfig { ThrowExceptions = false },
        new(new SystemEntityTypeName(error), TestingDefaults.CRON_EVERY_SECOND, _ => throw new Exception(error)),
        DateTime.MinValue);
  }

  public static Task<ReadOperationResult> GetEmptyResult(OperationStateAndConfig<ReadOperationConfig> _) => 
      Task.FromResult<ReadOperationResult>(new EmptyReadOperationResult());
}

public class TestingStagedEntityRepository() : InMemoryStagedEntityRepository(0, Helpers.TestingStagedEntityChecksum) { public List<StagedEntity> Contents => saved.ToList(); }

public class TestingInMemoryBaseCtlRepository : InMemoryBaseCtlRepository, ITestingCtlRepository {
  public Dictionary<(SystemName, LifecycleStage), SystemState> Systems => systems;
  public Dictionary<(SystemName, LifecycleStage, ObjectName), ObjectState> Objects => objects;
  public Dictionary<Map.Key, string> Maps => maps;
  
  public Task<List<Map.CoreToSysMap>> GetAllMaps() => 
      Task.FromResult(maps.Values.Select(Deserialize).Cast<Map.CoreToSysMap>().ToList());
}