using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Core.Runner;

public interface IFunctionInitialiser {

  public void RegisterServices(IServiceCollection svcs);

}