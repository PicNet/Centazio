using Centazio.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Centazio.Providers.EF;

public abstract class CentazioDbContext : DbContext {

  protected sealed override void OnConfiguring(DbContextOptionsBuilder builder) => 
      ConfigureDbSpecificOptions(builder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
  
  protected sealed override void OnModelCreating(ModelBuilder builder) {
    base.OnModelCreating(builder);

    builder.Model.GetEntityTypes().ForEach(etype => etype.GetProperties().Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)).ForEach(p => 
        p.SetValueConverter(new ValueConverter<DateTime, DateTime>(
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc), v => DateTime.SpecifyKind(v, DateTimeKind.Utc)))));
    
    CreateCentazioModel(builder);
  }
  
  protected abstract void CreateCentazioModel(ModelBuilder builder);
  protected abstract void ConfigureDbSpecificOptions(DbContextOptionsBuilder builder);
  
  public async Task<int> ToDtoAttachAndUpdate<E, D>(IEnumerable<E> entities) 
      where E : class 
      where D : class, IDto<E> {
    // todo: make DtoHelpers.ToDto return non nullable
    var dtos = entities.Select(DtoHelpers.ToDto).Cast<D>().ToList();
    AttachRange(dtos);
    dtos.ForEach(dto => Entry(dto).State = EntityState.Modified);
    return await SaveChangesAsync();
  }
  
  public async Task<int> ToDtoAttachAndCreate<E, D>(IEnumerable<E> entities) 
      where E : class 
      where D : class, IDto<E> {
    // todo: make DtoHelpers.ToDto return non nullable
    var dtos = entities.Select(DtoHelpers.ToDto).Cast<D>().ToList();
    await AddRangeAsync(dtos);
    return await SaveChangesAsync();
  }
}