namespace Centazio.Test.Lib.E2E.Sim;

public interface ISimulationInstructions {
  List<CrmCustomer> AddedCrmCustomers { get; }
  List<CrmCustomer> EditedCrmCustomers { get; }
  List<CrmInvoice> AddedCrmInvoices { get; }
  List<CrmInvoice> EditedCrmInvoices { get; }
  List<CrmMembershipType> EditedCrmMemberships { get; }
  
  List<FinAccount> AddedFinAccounts { get; }
  List<FinAccount> EditedFinAccounts { get; }
  List<FinInvoice> AddedFinInvoices { get; }
  List<FinInvoice> EditedFinInvoices { get; }
  
  void Init(SimulationCtx ctx, CrmDb crmdb, FinDb findb);
  bool HasMoreEpochs(int epoch);
  void Step(int epoch);
}