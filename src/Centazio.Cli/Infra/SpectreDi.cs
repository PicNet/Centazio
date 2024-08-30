using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace Centazio.Cli.Infra;

public sealed class TypeRegistrar(IServiceCollection builder) : ITypeRegistrar {

  public ITypeResolver Build() => new TypeResolver(builder.BuildServiceProvider());

  public void Register(Type service, Type implementation) => builder.AddSingleton(service, implementation);
  public void RegisterInstance(Type service, object implementation) => builder.AddSingleton(service, implementation);

  public void RegisterLazy(Type service, Func<object> func) {
    ArgumentNullException.ThrowIfNull(func);
    builder.AddSingleton(service, _ => func());
  }

}

public sealed class TypeResolver(IServiceProvider provider) : ITypeResolver, IDisposable {

  public void Dispose() => (provider as IDisposable)?.Dispose();
  public object? Resolve(Type? type) => type is null ? null : provider.GetService(type);

}