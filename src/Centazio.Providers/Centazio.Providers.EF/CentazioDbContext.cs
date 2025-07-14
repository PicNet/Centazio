using Centazio.Core.Misc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Serilog;

namespace Centazio.Providers.EF;

public abstract class CentazioDbContext : DbContext {

  private static readonly bool DEBUG = false;
  
  protected sealed override void OnConfiguring(DbContextOptionsBuilder builder) {
    if (DEBUG) builder.LogTo(Log.Debug);
    ConfigureDbSpecificOptions(builder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
  }

  protected sealed override void OnModelCreating(ModelBuilder builder) {
    CreateCentazioModel(builder);
    
    base.OnModelCreating(builder);

    builder.Model.GetEntityTypes().ForEach(etype => etype.GetProperties().Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)).ForEach(p => 
        p.SetValueConverter(new ValueConverter<DateTime, DateTime>(
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc), v => DateTime.SpecifyKind(v, DateTimeKind.Utc)))));
  }
  
  protected abstract void CreateCentazioModel(ModelBuilder builder);
  protected abstract void ConfigureDbSpecificOptions(DbContextOptionsBuilder options);
  
  // note: if you get a DbUpdateConcurrencyException from EF, check that the entities do exist
  //    in the DB.  You may need to call  ToDtoAttachAndCreate instead
  public async Task<int> ToDtoAttachAndUpdate<E, D>(IEnumerable<E> entities) 
      where E : class 
      where D : class, IDto<E> {
    var dtos = entities.Select(DtoHelpers.ToDto<E, D>).ToList();
    AttachRange(dtos);
    dtos.ForEach(dto => Entry(dto).State = EntityState.Modified);
    return await SaveChangesAsync();
  }
  
  public async Task<int> ToDtoAttachAndCreate<E, D>(IEnumerable<E> entities) 
      where E : class 
      where D : class, IDto<E> {
    var dtos = entities.Select(DtoHelpers.ToDto<E, D>).ToList();
    await AddRangeAsync(dtos);
    return await SaveChangesAsync();
  }
  
  public async Task CreateDb() => 
      await ExecSql(Database.GenerateCreateScript());

  public async Task DropDb(params List<TableName> tables) {
    var sql = String.Join(";\n", tables.Select(table => $"drop table if exists {ToTableName(table)}"));
    await ExecSql(sql);
  }
  
  protected abstract string ToTableName(TableName table); 
  
  public async Task ExecSql(string sql) {
    try {
      await sql.Split("\nGO\n").Select(async statement => {
        await Database.ExecuteSqlRawAsync(statement);
      }).Synchronous();
    }
    catch (Exception e) { Log.Error($"error [{e.Message}] executing sql:\n\t" + sql); throw; }
  }
}

public record TableName(string Schema, string Table);