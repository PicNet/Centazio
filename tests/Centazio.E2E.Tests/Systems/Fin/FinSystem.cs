namespace Centazio.E2E.Tests.Systems.Fin;

public class FinSystem : ISystem {

  public void Step() { throw new NotImplementedException(); }

}

public record FInvoices(int Id, int AccountId, decimal Amount, DateTime DueDate, DateTime PaidDate);
public record FAccount(int Id, string Name, List<FInvoices> Invoices);