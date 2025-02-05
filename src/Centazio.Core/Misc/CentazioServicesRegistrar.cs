using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Centazio.Core.Misc;

public class CentazioServicesRegistrar(IServiceCollection svcs) {
  public void Register<T>() where T : class => svcs.TryAddSingleton<T>();
  public void Register(Type t) => svcs.TryAddSingleton(t);
  public void Register<T>(T instance) where T : class => svcs.TryAddSingleton(instance);
  public void Register<T>(Func<IServiceProvider, T> factory) where T : class => svcs.TryAddSingleton(factory);
  public void Register(Type type, object instance) => svcs.TryAdd(ServiceDescriptor.Singleton(type, instance));
  public void RegisterServiceTypeFactory(Type type, Type factorytype) => svcs.TryAddSingleton(type, factorytype);
  public ServiceProvider BuildServiceProvider() => svcs.BuildServiceProvider();
}