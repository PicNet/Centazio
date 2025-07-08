using Centazio.Core.Settings;
using Centazio.Core.Stage;
using Serilog;
using Serilog.Events;

namespace Centazio.Test.Lib.E2E;

public class SimulationCtx : IAsyncDisposable {
  
  private readonly ISimulationStorage storage;
  
  public ICtlRepository CtlRepo { get; set; } = null!;
  public IStagedEntityRepository StageRepository { get; set; } = null!;
  public ISimulationCoreStorageRepository CoreStore { get; set; } = null!;
  public CentazioSettings Settings { get; set; }
  
  public IChecksumAlgorithm ChecksumAlg { get; }
  public EpochTracker Epoch { get; set; }
  public EntityConverter Converter { get; set; } = null!;

  public SimulationCtx(ISimulationStorage storage, CentazioSettings settings) {
    this.storage = storage;
    
    Settings = settings;
    ChecksumAlg = new Sha256ChecksumAlgorithm();
    Epoch = new(this);
  }
  
  public async Task Initialise() {
    await storage.Initialise(this);
    
    CtlRepo = storage.CtlRepo;
    StageRepository = storage.StageRepository;
    CoreStore = storage.CoreStore;
    
    Converter = new(CtlRepo);
  }
  
 
  public void Debug(string message, List<string>? details = null) {
    if (SC.SILENCE_SIMULATION) return;
    if (LogInitialiser.LevelSwitch.MinimumLevel < LogEventLevel.Fatal) Log.Information(message + DetailsToString(details));
    else DevelDebug.WriteLine(message);
  }
  
  public string DetailsToString(List<string>? details) {
    var detailslst = details?.ToList() ?? [];
    return !detailslst.Any() ? String.Empty : ":\n\t" + String.Join("\n\t", detailslst);
  }
  
  public SystemEntityId NewGuidSeid() => new (Rng.NewGuid().ToString());
  public SystemEntityId NewIntSeid() => new (Rng.Next(Int32.MaxValue).ToString());
  
  public string NewName<T>(string prefix, List<T> target, int idx) => $"{prefix}_{target.Count + idx}:0";
  
  public string UpdateName(string name) {
    var (label, count, _) = name.Split(':');
    return $"{label}:{Int32.Parse(count) + 1}";
  }

  public async ValueTask DisposeAsync() {
    await storage.DisposeAsync();
  }
}