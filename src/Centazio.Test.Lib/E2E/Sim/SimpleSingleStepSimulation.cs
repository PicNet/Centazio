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
      AddedCrmCustomers = [new CrmCustomer(ctx.NewGuidSeid(), UtcDate.UtcNow, Rng.RandomItem(crmdb.MembershipTypes).SystemId, "CrmCustomer1")];
      crmdb.Customers.AddRange(AddedCrmCustomers);

      TestingUtcDate.DoTick();
      AddedFinAccounts = [new FinAccount(ctx.NewIntSeid(), "FinAccount1", UtcDate.UtcNow)];
      findb.Accounts.AddRange(AddedFinAccounts);

      return;
    }

    throw new Exception();
  }

  public void Init(SimulationCtx ctx2, CrmDb crmdb2, FinDb findb2) {
    (ctx, crmdb, findb) = (ctx2, crmdb2, findb2);
    crmdb.MembershipTypes.Add(new CrmMembershipType(SimulationConstants.PENDING_MEMBERSHIP_TYPE_ID, UtcDate.UtcNow, "Membership"));
  }

}