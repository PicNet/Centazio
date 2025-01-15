using Centazio.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Sample.ClickUp;

public class ClickUpIntegrations : IntegrationBase<SampleSettings, SampleSecrets> {

  // todo: dynamically load functions
  public override List<Type> GetAllFunctionTypes() => [typeof(ClickUpReadFunction), typeof(ClickUpPromoteFunction)];

  protected override void RegisterIntegrationSpecificServices(IServiceCollection svcs) {
    svcs.AddSingleton<ClickUpApi>();
  }

}