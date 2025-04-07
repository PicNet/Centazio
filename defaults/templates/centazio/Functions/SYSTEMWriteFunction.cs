using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;
using Centazio.Core.Write;

namespace {{ it.Namespace }};

public class {{ it.SystemName }}WriteFunction(ICoreStorage core, ICtlRepository ctl, {{ it.SystemName }}Api api) : WriteFunction({{ it.SystemName }}Constants.{{ it.SystemName }}SystemName, core, ctl) {

  protected override FunctionConfig GetFunctionConfiguration() => new([
    new WriteOperationConfig(System, CoreEntityTypes.ExampleEntity, CronExpressionsHelper.EveryXSeconds(5), CovertExampleEntitiesTo{{ it.SystemName }}ExampleEntities, Write{{ it.SystemName }}ExampleEntities)
  ]);

  private Task<CovertCoresToSystemsResult> CovertExampleEntitiesTo{{ it.SystemName }}ExampleEntities(WriteOperationConfig config, List<CoreAndPendingCreateMap> tocreate, List<CoreAndPendingUpdateMap> toupdate) =>
      Task.FromResult(CovertCoresToSystems<ExampleEntity>(tocreate, toupdate, (id, e) => new {{ it.SystemName }}ExampleEntity(id.Value, e.Name, UtcDate.ToMillis().ToString())));

  private Task<WriteOperationResult> Write{{ it.SystemName }}ExampleEntities(WriteOperationConfig config, List<CoreSystemAndPendingCreateMap> tocreate, List<CoreSystemAndPendingUpdateMap> toupdate) => 
      WriteOperationResult.Create<ExampleEntity, {{ it.SystemName }}ExampleEntity>(tocreate, toupdate, CreateExampleEntities, UpdateExampleEntities);

  private async Task<List<Map.Created>> CreateExampleEntities<C, S>(List<CoreSystemAndPendingCreateMap<C, S>> tocreate) where C : ICoreEntity where S : ISystemEntity {
    var newids = await Task.WhenAll(tocreate.Select(async sysent => await api.CreateExampleEntity(sysent.SystemEntity.To<{{ it.SystemName }}ExampleEntity>().name)));
    return newids.Select((id, idx) => tocreate[idx].SuccessCreate(new (id))).ToList();
  }

  private async Task<List<Map.Updated>> UpdateExampleEntities<C, S>(List<CoreSystemAndPendingUpdateMap<C, S>> toupdate) where C : ICoreEntity where S : ISystemEntity {
    var updated = await Task.WhenAll(toupdate.Select(async sysent => {
      var entity = sysent.SystemEntity.To<{{ it.SystemName }}ExampleEntity>();
      await api.UpdateExampleEntity(entity.id, entity.name);
      return entity;
    }));
    return updated.Select((_, idx) => toupdate[idx].SuccessUpdate()).ToList();
  }
}

