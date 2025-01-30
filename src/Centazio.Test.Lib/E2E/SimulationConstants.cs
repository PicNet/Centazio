using Centazio.Core.Misc;
using Centazio.Core.Types;
using Centazio.Test.Lib.E2E.Crm;
using Centazio.Test.Lib.E2E.Fin;

namespace Centazio.Test.Lib.E2E;

[IgnoreNamingConventions]
public static class SimulationConstants {
  public static readonly SystemName CRM_SYSTEM = new(nameof(CrmApi));
  public static readonly SystemName FIN_SYSTEM = new(nameof(FinApi));
  
  public static readonly bool SILENCE_LOGGING = false;
  public static readonly bool SILENCE_SIMULATION = false;
  public static List<string> LOGGING_FILTERS { get; } = [];
  public static readonly int RANDOM_SEED = 999;
  
  public static readonly int TOTAL_EPOCHS = 100;
  
  public static readonly SystemEntityId PENDING_MEMBERSHIP_TYPE_ID = new(Rng.NewGuid().ToString());
  public static readonly int CRM_MAX_EDIT_MEMBERSHIPS = 2;
  public static readonly int CRM_MAX_NEW_CUSTOMERS = 4;
  public static readonly int CRM_MAX_EDIT_CUSTOMERS = 4;
  public static readonly int CRM_MAX_NEW_INVOICES = 4;
  public static readonly int CRM_MAX_EDIT_INVOICES = 4;
  
  public static readonly int FIN_MAX_NEW_ACCOUNTS = 2;
  public static readonly int FIN_MAX_EDIT_ACCOUNTS = 4;
  public static readonly int FIN_MAX_NEW_INVOICES = 2;
  public static readonly int FIN_MAX_EDIT_INVOICES = 2;
}