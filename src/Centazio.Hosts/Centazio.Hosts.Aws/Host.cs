using Amazon.Lambda.RuntimeSupport;
using Centazio.Core.Misc;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Centazio.Hosts.Aws;

public class Host {
  public async Task Init(IAwsFunctionHandler handler) {
    Log.Logger = LogInitialiser.GetConsoleConfig().CreateLogger();
    
    var svcs = new ServiceCollection();
    svcs.AddLogging(builder => builder.AddSerilog(Log.Logger));
    
    using var wrapper = HandlerWrapper.GetHandlerWrapper(handler.Handle);
    using var bootstrap = new LambdaBootstrap(wrapper);
    await bootstrap.RunAsync();
  }
}