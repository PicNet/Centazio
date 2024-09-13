using Centazio.Core;
using Centazio.Core.Tests.CoreRepo;
using Centazio.Core.Tests.IntegrationTests;

namespace Centazio.Providers.SqlServer.Tests.Ctl;

public class SqlServerCoreRepositoryTests() : CoreStorageRepositoryDefaultTests(false) {
  
  protected override async Task<ICoreStorageRepository> GetRepository() => await new TestingSqlServerCoreStorageRepository().Initalise();
}

public record CoreCustomerRaw {
  public string? Id { get; init; }
  public string? Checksum { get; init; }
  public string? FirstName { get; init; }
  public string? LastName { get; init; }
  public DateOnly? DateOfBirth { get; init; }
  public DateTime? DateUpdated { get; init; } 
  public string? SourceSystem { get; init; } 
  public DateTime? DateCreated { get; init; } 
  public DateTime? SourceSystemDateUpdated { get; init; }
  
  public static explicit operator CoreCustomer(CoreCustomerRaw raw) => new(
      raw.Id ?? throw new ArgumentNullException(nameof(Id)),
      raw.Checksum ?? "",
      raw.FirstName ?? throw new ArgumentNullException(nameof(FirstName)),
      raw.LastName ?? throw new ArgumentNullException(nameof(LastName)),
      raw.DateOfBirth ?? throw new ArgumentNullException(nameof(DateOfBirth)),
      raw.DateUpdated ?? UtcDate.UtcNow);
}