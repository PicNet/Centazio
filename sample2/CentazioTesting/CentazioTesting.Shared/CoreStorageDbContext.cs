using Centazio.Providers.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CentazioTesting.Shared;

public class CoreStorageDbContext(string connstr) : SqliteDbContext(connstr) {

  protected override void CreateCentazioModel(ModelBuilder builder) => builder
      .HasDefaultSchema("dbo")
      .Entity<CoreStorageMeta.Dto>(e => {
        e.ToTable(nameof(CoreStorageMeta).ToLower(), "ctl");
        e.HasKey(e2 => new { e2.CoreEntityTypeName, e2.CoreId });
      })
      .Entity<ExampleEntity.Dto>(e => {
        e.ToTable(nameof(ExampleEntity).ToLower());
        e.HasKey(e2 => e2.CoreId);
      });

}