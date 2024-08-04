using Centazio.Core;
using Centazio.Core.Tests.Api;

namespace centazio.core.tests.DummySystems.Crm;

public static class Constants {
  public static readonly SystemName CrmSystemName = new(nameof(Crm));
  public static readonly ObjectName CrmCustomer = new(nameof(Customer));
}