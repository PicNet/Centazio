using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreToSystemMapping;
using Centazio.Core.Ctl;
using Centazio.Core.Stage;
using Centazio.E2E.Tests.Infra;
using Centazio.Providers.Sqlite.CoreToSystemMapping;
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
  public ICtlRepository CtlRepo { get; }
  public ICoreToSystemMapStore EntityMap { get; }
  public IChecksumAlgorithm ChecksumAlg { get; }
  public EpochTracker Epoch { get; set; }
  public CoreStorage CoreStore { get; } 
  public IStagedEntityStore StageStore { get; }
  public EntityConverter Converter { get; } 
  
  internal SimulationCtx() {
    CtlRepo = new SqliteCtlRepository(() => sqliteconn);
    EntityMap = new SqliteCoreToSystemMapStore(() => sqliteconn);
    ChecksumAlg = new Sha256ChecksumAlgorithm();
    CoreStore = new(this);
    StageStore = new SqliteStagedEntityStore(() => sqliteconn, 0, ChecksumAlg.Checksum);
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