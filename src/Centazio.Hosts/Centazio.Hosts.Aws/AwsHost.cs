using Amazon.Lambda.RuntimeSupport;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Centazio.Hosts.Aws;

public static class AwsHost {
  private static AwsHostImpl? impl;
  public static async Task Init(List<string> environments, IAwsFunctionHandler handler, Type func) {
    if (impl is not null) throw new Exception("AwsHost.Init already called");
    impl = new(environments, func);
    await impl.Init(handler);
  }
  
  public static async Task RunFunction(List<FunctionTrigger> triggers) {
    if (impl is null) throw new Exception("AzHost.Init has not been called");
    await impl.RunFunction(triggers);
  }
}

public class AwsHostImpl(List<string> environments, Type func) {

  private readonly AwsHostInitialiser centazio = new(environments);

  public async Task Init(IAwsFunctionHandler handler) {
    InitLogger();
    await centazio.Init([func]);
    await InitAwsHost(handler);
    
  }

  private void InitLogger() {
    // todo: add cloudwatch support
    Log.Logger = LogInitialiser.GetConsoleConfig().CreateLogger();
    
    // todo: we do not call svcs.Build() so there is no point in this?
    var svcs = new ServiceCollection();
    svcs.AddLogging(builder => builder.AddSerilog(Log.Logger));
  }

  private async Task InitAwsHost(IAwsFunctionHandler handler) {
    using var wrapper = HandlerWrapper.GetHandlerWrapper(handler.Handle);
    using var bootstrap = new LambdaBootstrap(wrapper);
    await bootstrap.RunAsync();
  }

  public async Task RunFunction(List<FunctionTrigger> triggers) => 
      await centazio.GetRunner().RunFunction(centazio.GetFunction(func), triggers);

}
