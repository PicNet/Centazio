using Centazio.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Sample.ClickUp;

public class ClickUpIntegrations : IntegrationBase<SampleSettings, SampleSecrets> {

  public override List<Type> GetAllFunctionTypes() => [typeof(ClickUpReadFunction)];

  protected override void RegisterIntegrationSpecificServices(IServiceCollection svcs) {
    svcs.AddSingleton<ClickUpApi>();
  }

}