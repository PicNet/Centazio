using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.Ctl;
using Centazio.Core.Stage;
using Centazio.E2E.Tests.Infra;
using Centazio.E2E.Tests.Systems;
using Serilog;
using Serilog.Events;
using C = Centazio.E2E.Tests.SimulationConstants;

namespace Centazio.E2E.Tests;

public class SimulationCtx : IAsyncDisposable {
  
  public ICtlRepository CtlRepo { get; } = new InMemoryCtlRepository();
  public TestingInMemoryCoreToSystemMapStore EntityMap { get; } = new();
  public IChecksumAlgorithm ChecksumAlg { get; }
  public EpochTracker Epoch { get; set; }
  public CoreStorage CoreStore { get; } 
  public IStagedEntityStore StageStore { get; }
  public EntityConverter Converter { get; } 
  
  // todo: these helpers should be reuseable `Centazio.Core` classes
  public FunctionHelpers CrmHelpers { get; } 
  public FunctionHelpers FinHelpers { get; } 

  internal SimulationCtx() {
    ChecksumAlg = new Sha256ChecksumAlgorithm();
    CoreStore = new(this);
    StageStore = new InMemoryStagedEntityStore(0, ChecksumAlg.Checksum);
    CrmHelpers = new FunctionHelpers(C.CRM_SYSTEM, ChecksumAlg, EntityMap);
    FinHelpers = new FunctionHelpers(C.FIN_SYSTEM, ChecksumAlg, EntityMap);
    Converter = new(EntityMap);
    Epoch = new(0, this);
  }
 
  public void Debug(string message, params object[] args) {
    if (C.SILENCE_SIMULATION) return;
    if (LogInitialiser.LevelSwitch.MinimumLevel < LogEventLevel.Fatal) Log.Information(message, args);
    else DevelDebug.WriteLine(message);
  }
  
  
  public string NewName<T>(string prefix, List<T> target, int idx) => $"{prefix}_{target.Count + idx}:0";
  
  public string UpdateName(string name) {
    var (label, count, _) = name.Split(':');
    return $"{label}:{Int32.Parse(count) + 1}";
  }

  public async ValueTask DisposeAsync() {
    await CtlRepo.DisposeAsync();
    await EntityMap.DisposeAsync();
    await CoreStore.DisposeAsync();
    await StageStore.DisposeAsync();
  }

}