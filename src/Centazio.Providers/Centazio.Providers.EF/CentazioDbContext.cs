using Centazio.Core;
using Centazio.Core.Misc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Serilog;

namespace Centazio.Providers.EF;

public abstract class CentazioDbContext : DbContext {

  protected sealed override void OnConfiguring(DbContextOptionsBuilder builder) => 
      ConfigureDbSpecificOptions(builder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

  protected override void ConfigureConventions(ModelConfigurationBuilder builder) {
    builder.Conventions.Add(_ => new ValidStringConvention(ValidString.AllSubclasses()));
  }

  private class ValidStringConverter<T>() : ValueConverter<T, string>(
      vs => vs.Value,
      str => (T) Activator.CreateInstance(typeof(T), str)!)
      where T : ValidString;

  private class ValidStringConvention(List<Type> vstypes) : IModelFinalizingConvention {
    public void ProcessModelFinalizing(IConventionModelBuilder builder, IConventionContext<IConventionModelBuilder> ctx) {
      builder.Metadata.GetEntityTypes().ForEach(etype => {
        etype.GetProperties().ForEach(prop => {
          var ptype = prop.ClrType;
          var realtype = Nullable.GetUnderlyingType(ptype) ?? ptype;
          if (!vstypes.Contains(realtype)) return;

          var convertertype = typeof(ValidStringConverter<>).MakeGenericType(realtype);
          var converter = (ValueConverter) (Activator.CreateInstance(convertertype) ?? throw new Exception());
          prop.SetValueConverter(converter);
        });
      });
    }
  }

  
  protected sealed override void OnModelCreating(ModelBuilder builder) {
    CreateCentazioModel(builder);
    
    base.OnModelCreating(builder);

    ValidString.AllSubclasses().ForEach(vs => builder.Ignore(vs));
    
    builder.Model.GetEntityTypes()
        .ForEach(ApplyDateTimeValueConvertersToEntityType);
    
    void ApplyDateTimeValueConvertersToEntityType(IMutableEntityType type) {
      var props = type.GetProperties().ToList();
      var dates = props.Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)).ToList();
      
      dates.ForEach(p => 
        p.SetValueConverter(new ValueConverter<DateTime, DateTime>(
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc), v => DateTime.SpecifyKind(v, DateTimeKind.Utc))));
    }
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