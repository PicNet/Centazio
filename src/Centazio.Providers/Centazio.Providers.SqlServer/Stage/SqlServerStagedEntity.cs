using Centazio.Core;
using Centazio.Core.Entities.Ctl;

namespace Centazio.Providers.SQLServer.Stage;

public record SqlServerStagedEntity(Guid Id, SystemName SourceSystem, ObjectName Object, DateTime DateStaged, string Data, DateTime? DatePromoted = null, string? Ignore = null) 
    : StagedEntity(SourceSystem, Object, DateStaged, Data, DatePromoted, Ignore) {
  
  public static SqlServerStagedEntity FromStagedEntity(StagedEntity se) => 
        se as SqlServerStagedEntity 
        ?? new SqlServerStagedEntity(Guid.CreateVersion7(), se.SourceSystem, se.Object, se.DateStaged, se.Data, se.DatePromoted, se.Ignore);
}
    