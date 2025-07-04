using Centazio.Core;
using Centazio.Core.Misc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Serilog;
using System.Reflection;

namespace Centazio.Providers.EF;

public abstract class CentazioDbContext : DbContext {

  protected sealed override void OnConfiguring(DbContextOptionsBuilder builder) => 
      ConfigureDbSpecificOptions(builder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

  protected override void ConfigureConventions(ModelConfigurationBuilder builder) => 
      ValidString.AllSubclasses().ForEach(t => RegisterValidStringType(builder, t));

  private static void RegisterValidStringType(ModelConfigurationBuilder builder, Type validStringType) {
    typeof(CentazioDbContext)
        .GetMethod(nameof(RegisterValidStringTypeGeneric), BindingFlags.NonPublic | BindingFlags.Static)!
        .MakeGenericMethod(validStringType)
        .Invoke(null, [builder]);
  }

  private static void RegisterValidStringTypeGeneric<T>(ModelConfigurationBuilder builder) where T : ValidString => 
      builder.Properties<T>().HaveConversion<ValidStringConverter<T>>();

  // ReSharper disable once ClassNeverInstantiated.Local
  private class ValidStringConverter<T>() : ValueConverter<T, string>(
      vs => vs.Value,
      str => (T) Activator.CreateInstance(typeof(T), str)!)
      where T : ValidString;

  protected sealed override void OnModelCreating(ModelBuilder builder) {
    CreateCentazioModel(builder);
    
    base.OnModelCreating(builder);

    builder.Model.GetEntityTypes()
        .SelectMany(t => t.GetProperties())
        .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?))
        .ForEach(p => 
            p.SetValueConverter(new ValueConverter<DateTime, DateTime>(
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc), 
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc))));
  }

  protected abstract void CreateCentazioModel(ModelBuilder builder);
  protected abstract void ConfigureDbSpecificOptions(DbContextOptionsBuilder options);
  

  // note: if you get a DbUpdateConcurrencyException from EF, check that the entities do exist
  //    in the DB.  You may need to call  ToDtoAttachAndCreate instead
  public async Task<int> ToDtoAttachAndUpdate<E>(IEnumerable<E> entities) 
      where E : class {
    var lst = entities.ToList();
    AttachRange(lst);
    lst.ForEach(dto => Entry(dto).State = EntityState.Modified);
    return await SaveChangesAsync();
  }
  
  public async Task<int> ToDtoAttachAndCreate<E>(IEnumerable<E> entities) where E : class {
    await AddRangeAsync(entities);
    return await SaveChangesAsync();
  }
  
  public async Task ExecSql(string sql) {
    try { await Database.ExecuteSqlRawAsync(sql); }
    catch (Exception e) { Log.Error($"error [{e.Message}] executing sql:\n\t" + sql); throw; }
  }
}