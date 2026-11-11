using Centazio.Core.Promote;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Core.Stage;
using Centazio.Core.Write;
using Centazio.Providers.Aws.Secrets;
using Centazio.Test.Lib.InMemRepos;
using Microsoft.Extensions.Time.Testing;

namespace Centazio.Test.Lib;

public static class TestingFactories {
  
  // every factory that builds a repository, runner or function takes the test's clock, a silently created second
  //    clock would let parts of one test see different times
  public static FakeTimeProvider Clock() => new(new DateTimeOffset(TestingDefaults.DefaultStartDt));
  public static IGuidGenerator Ids() => new SeededGuidGenerator(new Random(TestingDefaults.SEED));
  
  public static async Task<CentazioSettings> Settings(params List<string> environments) => await Settings<CentazioSettings>(environments);
  public static async Task<E> Settings<E>(params List<string> environments) => await new SettingsLoader().Load<E>(environments.Any() ? environments : [CentazioConstants.DEFAULT_ENVIRONMENT]);
  
  public static async Task<CentazioSecrets> Secrets() => await Secrets<CentazioSecrets>();
  public static async Task<E> Secrets<E>() => await new AwsSecretsLoader(await Settings()).Load<E>(CentazioConstants.DEFAULT_ENVIRONMENT);
  
  public static TestingStagedEntityRepository SeRepo(TimeProvider clock) => new(clock); 
  public static TestingInMemoryBaseCtlRepository CtlRepo(TimeProvider clock) => new(clock);
  public static TestingInMemoryCoreStorageRepository CoreRepo() => new();
  public static async Task<FunctionRunResults> RunFunc<OC>(TimeProvider clock, AbstractFunction<OC> func, ICtlRepository ctl) where OC : OperationConfig => 
      await (await FuncRunner(clock, ctl)).RunFunction(func, [new TimerChangeTrigger(func.Config.FunctionPollExpression ?? String.Empty)]);
  public static async Task<FunctionRunner> FuncRunner(TimeProvider clock, ICtlRepository? ctl = null) => new(ctl ?? CtlRepo(clock), await Settings(), clock, true); 
  public static ReadFunction ReadFunc(
      TimeProvider clock,
      IEntityStager? stager = null, 
      ICtlRepository? ctl = null) => new EmptyReadFunction(new (nameof(TestingFactories)), stager ?? SeRepo(clock), ctl ?? CtlRepo(clock), clock);
  public static FunctionConfig EmptyFunctionConfig() => new([new ReadOperationConfig(new(nameof(TestingFactories)), CronExpressionsHelper.EverySecond(), _ => null!)]);
      
  public static PromoteFunction PromoteFunc(
      TimeProvider clock,
      IStagedEntityRepository? serepo = null, 
      ICtlRepository? ctl = null, 
      ICoreStorage? core = null) => 
      new EmptyPromoteFunction(new (nameof(TestingFactories)), serepo ?? SeRepo(clock), core ?? CoreRepo(), ctl ?? CtlRepo(clock), clock);

  public static WriteFunction WriteFunc(
      TimeProvider clock,
      IStagedEntityRepository? serepo = null, 
      ICtlRepository? ctl = null, 
      ICoreStorage? core = null) => 
      new EmptyWriteFunction(new (nameof(TestingFactories)), core ?? CoreRepo(), ctl ?? CtlRepo(clock), clock);
  
  public static CoreEntityAndMeta NewCoreEntity(string first, string last, DateTime now, CoreEntityId id) {
    var dob = DateOnly.MinValue;
    var core = new CoreEntity(id, first, last, dob);
    return CoreEntityAndMeta.Create(C.System1Name, C.SystemEntityName, new (id.Value), core, Helpers.TestingCoreEntityChecksum(core), now);
  }

  public static async Task<OperationStateAndConfig<ReadOperationConfig>> CreateReadOpStateAndConf(TimeProvider clock, ICtlRepository repo) {
    var success = nameof(EOperationResult.Success);
    return new OperationStateAndConfig<ReadOperationConfig>(await repo.CreateObjectState(await repo.CreateSystemState(new(success), new(success)), new SystemEntityTypeName(success), clock.UtcNow()),
        EmptyFunctionConfig(),
        new(new SystemEntityTypeName(success), TestingDefaults.CRON_EVERY_SECOND, GetEmptyResult),
        DateTime.MinValue);
  }

  public static async Task<OperationStateAndConfig<ReadOperationConfig>> CreateErroringOpStateAndConf(TimeProvider clock, ICtlRepository repo) {
    var error = nameof(EOperationResult.Error);
    return new OperationStateAndConfig<ReadOperationConfig>(await repo.CreateObjectState(await repo.CreateSystemState(new(error), new(error)), new SystemEntityTypeName(error), clock.UtcNow()),
        EmptyFunctionConfig() with { ThrowExceptions = false },
        new(new SystemEntityTypeName(error), TestingDefaults.CRON_EVERY_SECOND, _ => throw new Exception(error)),
        DateTime.MinValue);
  }

  public static Task<ReadOperationResult> GetEmptyResult(OperationStateAndConfig<ReadOperationConfig> _) => 
      Task.FromResult<ReadOperationResult>(new EmptyReadOperationResult());
}

public class TestingStagedEntityRepository(TimeProvider clock) : InMemoryStagedEntityRepository(0, Helpers.TestingStagedEntityChecksum, clock, F.Ids()) { public List<StagedEntity> Contents => saved.ToList(); }

public class TestingInMemoryBaseCtlRepository(TimeProvider clock) : InMemoryBaseCtlRepository(clock), ITestingCtlRepository {
  public Dictionary<SystemStateKey, SystemState> Systems => systems;
  public Dictionary<ObjectStateKey, ObjectState> Objects => objects;
  public Dictionary<Map.Key, string> Maps => maps;
  
  public Task<List<Map.CoreToSysMap>> GetAllMaps() => 
      Task.FromResult(maps.Values.Select(Deserialize).Cast<Map.CoreToSysMap>().ToList());
}

public class EmptyReadFunction(SystemName system, IEntityStager stager, ICtlRepository ctl, TimeProvider clock) : ReadFunction(system, stager, ctl, clock) {

  protected override FunctionConfig GetFunctionConfiguration() => new([
    new ReadOperationConfig(C.SystemEntityName, CronExpressionsHelper.EverySecond(), GetUpdatesAfterCheckpoint)
  ]);

  private Task<ReadOperationResult> GetUpdatesAfterCheckpoint(OperationStateAndConfig<ReadOperationConfig> config) => 
      Task.FromResult<ReadOperationResult>(new EmptyReadOperationResult());

}

public class EmptyPromoteFunction(SystemName system, IStagedEntityRepository stage, ICoreStorage core, ICtlRepository ctl, TimeProvider clock) : PromoteFunction(system, stage, core, ctl, clock) {

  protected override FunctionConfig GetFunctionConfiguration() => new([
    new PromoteOperationConfig(System, typeof(System1Entity), C.SystemEntityName, C.CoreEntityName, CronExpressionsHelper.EverySecond(), BuildCoreEntities)
  ]);

  public Task<List<EntityEvaluationResult>> BuildCoreEntities(OperationStateAndConfig<PromoteOperationConfig> config, List<EntityForPromotionEvaluation> toeval) => 
      Task.FromResult(new List<EntityEvaluationResult>());

}

public class EmptyWriteFunction(SystemName system, ICoreStorage core, ICtlRepository ctl, TimeProvider clock) : WriteFunction(system, core, ctl, clock) {

  protected override FunctionConfig GetFunctionConfiguration() => new([
    new WriteOperationConfig(System, C.CoreEntityName, CronExpressionsHelper.EverySecond(), null!, null!)
  ]);
}

public class NoOpChangeNotifier : IChangesNotifier {

  public List<ObjectChangeTrigger> Notifications { get; set; } = [];
  public Task Setup(IRunnableFunction func) => Task.CompletedTask;
  public void Init(List<IRunnableFunction> functions) {}
  public Task Run(IFunctionRunner runner) => Task.CompletedTask;
  public Task Notify(SystemName system, LifecycleStage stage, List<ObjectName> objs) {
    Notifications = objs.Select(obj => new ObjectChangeTrigger(system, stage, obj)).ToList();
    return Task.CompletedTask;
  }

  public Task Init(IFunctionRunner runner, List<IRunnableFunction> functions) => Task.CompletedTask;

}

public class InstantChangesNotifier : IChangesNotifier {

  public Task Setup(IRunnableFunction func) => Task.CompletedTask;

  private IFunctionRunner? runner;
  private List<IRunnableFunction> functions = null!;

  public void Init(List<IRunnableFunction> funcs) {
    functions = funcs;
  }
  
  public Task Run(IFunctionRunner funrun) {
    runner = funrun;
    return Task.CompletedTask;
  }
  
  public async Task Notify(SystemName system, LifecycleStage stage, List<ObjectName> objs) {
    if (runner is null) throw new Exception();
    
    var triggers = objs.Distinct().Select(obj => new ObjectChangeTrigger(system, stage, obj)).ToList();
    var totrigger = NotifierUtils.GetFunctionsThatAreTriggeredByTriggers(triggers, functions);
    await totrigger.Select(func => runner.RunFunction(func.Function, func.ResponsibleTriggers)).Synchronous();
  }
}