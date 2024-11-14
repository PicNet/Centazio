using Centazio.Core.Runner;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Sample;

public class Initialiser : IFunctionInitialiser {

  public void RegisterServices(IServiceCollection svcs) {
    svcs.AddSingleton<DummySystemApi>();
  }

}