using Centazio.Providers.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Sample.Shared;

// tood: where is this used?
public class CoreStorageDbContext(string connstr) : SqliteDbContext(connstr) {

  protected override void CreateCentazioModel(ModelBuilder builder) => builder
      .HasDefaultSchema("dbo")
      .Entity<CoreStorageMeta>(e => {
        e.ToTable(nameof(CoreStorageMeta).ToLower(), "ctl");
        e.HasKey(e2 => new { e2.CoreEntityTypeName, e2.CoreId });
      })
      .Entity<CoreTask>(e => {
        e.ToTable(nameof(CoreTask).ToLower());
        e.HasKey(e2 => e2.CoreId);
      });

}