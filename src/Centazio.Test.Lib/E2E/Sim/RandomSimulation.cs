namespace Centazio.Test.Lib.E2E.Sim;

public class RandomSimulation : ISimulationInstructions {
  public static readonly int TOTAL_EPOCHS = 15;
  
  private SimulationCtx ctx = null!;
  
  private CrmSimulation crmsim = null!;

  public List<CrmCustomer> AddedCrmCustomers => crmsim.AddedCustomers;
  public List<CrmCustomer> EditedCrmCustomers => crmsim.EditedCustomers;
  public List<CrmInvoice> AddedCrmInvoices => crmsim.AddedInvoices;
  public List<CrmInvoice> EditedCrmInvoices => crmsim.EditedInvoices;
  public List<CrmMembershipType> EditedCrmMemberships => crmsim.EditedMemberships;
  
  private FinSimulation finsim = null!;
  
  public List<FinAccount> AddedFinAccounts => finsim.AddedAccounts;
  public List<FinAccount> EditedFinAccounts => finsim.EditedAccounts;
  public List<FinInvoice> AddedFinInvoices => finsim.AddedInvoices;
  public List<FinInvoice> EditedFinInvoices => finsim.EditedInvoices;
  
  public void Init(SimulationCtx simctx, CrmDb crmdb, FinDb findb) {
    ctx = simctx;
    crmsim = new CrmSimulation(ctx, crmdb);
    finsim = new FinSimulation(ctx, findb);
    
    crmdb.MembershipTypes.AddRange([
      Create("Pending:0", SC.PENDING_MEMBERSHIP_TYPE_ID),
      Create("Standard:0"),
      Create("Silver:0"),
      Create("Gold:0")
    ]);
    
    CrmMembershipType Create(string name, SystemEntityId? sysid = null) {
      sysid ??= ctx.NewGuidSeid(); 
      return new CrmMembershipType(sysid, CorrelationId.Build(SC.CRM_SYSTEM, sysid),  UtcDate.UtcNow, name);
    }
  }

  public bool HasMoreEpochs(int epoch) => epoch < TOTAL_EPOCHS;

  public void Step(int epoch) {
    crmsim.Step();
    finsim.Step();
    
    ctx.Debug($"Epoch: [{epoch}] simulation step completed\n\t" +
        $"CRM.Memberships[{EditedCrmMemberships.Count}] CRM.Customers[{AddedCrmCustomers.Count}/{EditedCrmCustomers.Count}] CRM.Invoices[{AddedCrmInvoices.Count}/{EditedCrmInvoices.Count}]\n\t" +
        $"FIN.Accounts[{AddedFinAccounts.Count}/{EditedFinAccounts.Count}] FIN.Invoices[{AddedFinInvoices.Count}/{EditedFinInvoices.Count}]");
  }
}