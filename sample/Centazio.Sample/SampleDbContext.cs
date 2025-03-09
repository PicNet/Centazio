using Centazio.Providers.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Sample;

public class SampleDbContext(string connstr) : SqliteDbContext(connstr) {

  protected override void CreateCentazioModel(ModelBuilder builder) => builder
      .HasDefaultSchema("dbo")
      .Entity<CoreStorageMeta.Dto>(e => {
        e.ToTable(nameof(CoreStorageMeta).ToLower(), nameof(Core.Ctl).ToLower());
        e.HasKey(e2 => new { e2.CoreEntityTypeName, e2.CoreId });
      })
      .Entity<CoreTask.Dto>(e => {
        e.ToTable(nameof(CoreTask).ToLower());
        e.HasKey(e2 => e2.CoreId);
      });

}