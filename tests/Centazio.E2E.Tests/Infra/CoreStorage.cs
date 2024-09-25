using Centazio.Core.CoreRepo;

namespace Centazio.E2E.Tests.Infra;

public record CoreCustomer : ICoreEntity {

  public string SourceSystem { get; }
  public string SourceId { get; }
  public string Id { get; }
  public string Checksum { get; }
  public DateTime DateCreated { get; }
  public DateTime DateUpdated { get; }
  public DateTime SourceSystemDateUpdated { get; }

}

public class CoreStorage {

  

}