namespace Centazio.Test.Lib.E2E;

[IgnoreNamingConventions]
public static class SimulationConstants {
  
  public static bool SILENCE_SIMULATION = false;
  public static readonly bool SILENCE_LOGGING = false;
  public static List<string> LOGGING_FILTERS { get; } = [DataFlowLogger.PREFIX, "Epoch:"];
  public static readonly int RANDOM_SEED = 999;
  
  public static class Crm {
    public static readonly SystemName SYSTEM_NAME = new(nameof(CrmApi));
    
    public static readonly SystemEntityTypeName MEMBERSHIP_TYPE = new(nameof(CrmMembershipType));
    public static readonly SystemEntityTypeName CUSTOMER = new(nameof(CrmCustomer));
    public static readonly SystemEntityTypeName INVOICE = new(nameof(CrmInvoice));
    
    public static readonly SystemEntityId PENDING_MEMBERSHIP_TYPE_ID = new(Rng.NewGuid().ToString());
  }
  
  public static class Fin {
    public static readonly SystemName SYSTEM_NAME = new(nameof(FinApi));
    
    public static readonly SystemEntityTypeName ACCOUNT = new(nameof(FinAccount));
    public static readonly SystemEntityTypeName INVOICE = new(nameof(FinInvoice));
  }
}