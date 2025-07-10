namespace Centazio.Test.Lib.E2E.Sim;

public class SimpleSingleStepSimulation : ISimulationInstructions {

  private CrmDb crmdb = null!;
  private SimulationCtx ctx = null!;
  private FinDb findb = null!;
  public List<CrmCustomer> AddedCrmCustomers { get; private set; } = [];
  public List<CrmMembershipType> EditedCrmMemberships { get; } = [];
  public List<FinAccount> AddedFinAccounts { get; private set; } = [];
  public List<CrmCustomer> EditedCrmCustomers { get; } = [];
  public List<CrmInvoice> AddedCrmInvoices { get; } = [];
  public List<CrmInvoice> EditedCrmInvoices { get; } = [];
  public List<FinAccount> EditedFinAccounts { get; } = [];
  public List<FinInvoice> AddedFinInvoices { get; } = [];
  public List<FinInvoice> EditedFinInvoices { get; } = [];
  public bool HasMoreEpochs(int epoch) => epoch < 1;

  public void Step(int epoch) {
    (AddedCrmCustomers, AddedFinAccounts) = ([], []);
    if (epoch == 0) {
      TestingUtcDate.DoTick();
      var crmsysid = ctx.NewGuidSeid();
      AddedCrmCustomers = [new CrmCustomer(crmsysid, CorrelationId.Build(SC.Crm.SYSTEM_NAME, SC.Crm.CUSTOMER, crmsysid),  UtcDate.UtcNow, Rng.RandomItem(crmdb.MembershipTypes).SystemId, "CrmCustomer1")];
      crmdb.Customers.AddRange(AddedCrmCustomers);

      TestingUtcDate.DoTick();
      var finsysid = ctx.NewIntSeid();
      AddedFinAccounts = [new FinAccount(finsysid, CorrelationId.Build(SC.Fin.SYSTEM_NAME, SC.Fin.ACCOUNT, finsysid), "FinAccount1", UtcDate.UtcNow)];
      findb.Accounts.AddRange(AddedFinAccounts);

      return;
    }

    throw new Exception();
  }

  public void Init(SimulationCtx ctx2, CrmDb crmdb2, FinDb findb2) {
    (ctx, crmdb, findb) = (ctx2, crmdb2, findb2);
    crmdb.MembershipTypes.Add(new CrmMembershipType(SC.Crm.PENDING_MEMBERSHIP_TYPE_ID, CorrelationId.Build(SC.Crm.SYSTEM_NAME, SC.Crm.MEMBERSHIP_TYPE, SC.Crm.PENDING_MEMBERSHIP_TYPE_ID), UtcDate.UtcNow, "Membership"));
  }

}