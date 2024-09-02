using Centazio.Core;
using Centazio.Core.Ctl.Entities;

namespace Centazio.Providers.SQLServer.Stage;

public record SqlServerStagedEntity(Guid Id, SystemName SourceSystem, ObjectName Object, DateTime DateStaged, string Data, string Checksum, DateTime? DatePromoted = null, string? Ignore = null) 
    : StagedEntity(SourceSystem, Object, DateStaged, Data, Checksum, DatePromoted, Ignore) {
  
  public static SqlServerStagedEntity FromStagedEntity(StagedEntity se) => 
        se as SqlServerStagedEntity 
        ?? new SqlServerStagedEntity(Guid.CreateVersion7(), se.SourceSystem, se.Object, se.DateStaged, se.Data, se.Checksum, se.DatePromoted, se.Ignore);
}
    