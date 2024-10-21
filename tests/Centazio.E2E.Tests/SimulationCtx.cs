﻿using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.Ctl;
using Centazio.Core.Stage;
using Centazio.E2E.Tests.Infra;
using Centazio.Providers.Sqlite.Ctl;
using Centazio.Providers.Sqlite.Stage;
using Microsoft.Data.Sqlite;
using Serilog;
using Serilog.Events;
using C = Centazio.E2E.Tests.SimulationConstants;

namespace Centazio.E2E.Tests;

public class SimulationCtx : IAsyncDisposable {
  
  private const string SIM_SQLITE_FILENAME = "centazio_simulation.db";
  private SqliteConnection sqliteconn => new($"Data Source={SIM_SQLITE_FILENAME};");
  public ICtlRepository CtlRepo { get; set; } = null!;
  public IStagedEntityRepository StageRepository { get; set; } = null!;
  public IChecksumAlgorithm ChecksumAlg { get; }
  public EpochTracker Epoch { get; set; }
  public CoreStorage CoreStore { get; set; } = null!; 
  public EntityConverter Converter { get; set; } = null!;

  public SimulationCtx() {
    ChecksumAlg = new Sha256ChecksumAlgorithm();
    Epoch = new(0, this);
  }
  
  public async Task Initialise() {
    File.Delete(SIM_SQLITE_FILENAME);
    
    CtlRepo = await new SqliteCtlRepository(() => sqliteconn).Initalise();
    StageRepository = await new SqliteStagedEntityRepository(() => sqliteconn, 0, ChecksumAlg.Checksum).Initalise();
    CoreStore = new(this);
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
    await CtlRepo.DisposeAsync();
    await CoreStore.DisposeAsync();
    await StageRepository.DisposeAsync();
  }

}