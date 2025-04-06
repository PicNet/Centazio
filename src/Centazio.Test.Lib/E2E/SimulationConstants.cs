namespace Centazio.Test.Lib.E2E;

[IgnoreNamingConventions]
public static class SimulationConstants {
  public static readonly SystemName CRM_SYSTEM = new(nameof(CrmApi));
  public static readonly SystemName FIN_SYSTEM = new(nameof(FinApi));
  
  public static bool SILENCE_SIMULATION = false;
  public static readonly bool SILENCE_LOGGING = false;
  public static List<string> LOGGING_FILTERS { get; } = [DataFlowLogger.PREFIX, "Epoch:"];
  public static readonly int RANDOM_SEED = 999;
  
  public static readonly SystemEntityId PENDING_MEMBERSHIP_TYPE_ID = new(Rng.NewGuid().ToString());
}