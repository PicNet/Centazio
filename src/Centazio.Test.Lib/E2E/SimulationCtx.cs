﻿using Centazio.Core.Checksum;
using Centazio.Core.Ctl;
using Centazio.Core.Misc;
using Centazio.Core.Stage;
using Serilog;
using Serilog.Events;
using C = Centazio.Test.Lib.E2E.SimulationConstants;

namespace Centazio.Test.Lib.E2E;

public class SimulationCtx : IAsyncDisposable {
  
  private readonly ISimulationProvider provider;
  
  public ICtlRepository CtlRepo { get; set; } = null!;
  public IStagedEntityRepository StageRepository { get; set; } = null!;
  public ISimulationCoreStorageRepository CoreStore { get; set; } = null!;
  
  public IChecksumAlgorithm ChecksumAlg { get; }
  public EpochTracker Epoch { get; set; }
  public EntityConverter Converter { get; set; } = null!;

  public SimulationCtx(ISimulationProvider provider) {
    this.provider = provider;
    
    ChecksumAlg = new Sha256ChecksumAlgorithm();
    Epoch = new(this);
  }
  
  public async Task Initialise() {
    await provider.Initialise(this);
    
    CtlRepo = provider.CtlRepo;
    StageRepository = provider.StageRepository;
    CoreStore = provider.CoreStore;
    
    Converter = new(CtlRepo);
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
    await provider.DisposeAsync();
  }
}