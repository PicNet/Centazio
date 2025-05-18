using Amazon.Lambda.RuntimeSupport;
using Centazio.Core.Misc;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Centazio.Hosts.Aws;

public class AwsHost {
  public async Task Init(IAwsFunctionHandler handler) {
    // todo: add cloudwatch support
    Log.Logger = LogInitialiser.GetConsoleConfig().CreateLogger();
    
    // todo: we do not call svcs.Build() so there is no point in this?
    var svcs = new ServiceCollection();
    svcs.AddLogging(builder => builder.AddSerilog(Log.Logger));
    
    using var wrapper = HandlerWrapper.GetHandlerWrapper(handler.Handle);
    using var bootstrap = new LambdaBootstrap(wrapper);
    await bootstrap.RunAsync();
  }
}