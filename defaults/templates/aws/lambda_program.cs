using Microsoft.Extensions.DependencyInjection;
using Amazon.Lambda.RuntimeSupport;
using Serilog;

using Centazio.Core.Misc;

namespace {{it.FunctionNamespace}}.Aws;

public class Program {
  public static async Task Main() {
    Log.Logger = LogInitialiser.GetConsoleConfig().CreateLogger();
    
    var svcs = new ServiceCollection();
    svcs.AddLogging(builder => builder.AddSerilog(Log.Logger));
    var prov = svcs.BuildServiceProvider();
    
    var handler = new {{it.ClassName}}Handler();
    using var wrapper = HandlerWrapper.GetHandlerWrapper(ctx => handler.Handle(ctx));
    using var bootstrap = new LambdaBootstrap(wrapper);
    await bootstrap.RunAsync();
  }
}