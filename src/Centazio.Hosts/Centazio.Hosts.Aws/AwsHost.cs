using Amazon.Lambda.RuntimeSupport;
using Centazio.Core.Runner;
using Centazio.Core.Settings;
using Serilog;
using Type = System.Type;

namespace Centazio.Hosts.Aws;

public static class AwsHost {

  private static AwsHostImpl? impl;

  public static async Task Init(List<string> environments, IAwsFunctionHandler handler, Type func) {
    if (impl is not null) {
      Log.Information("AwsHost.Init already called");
      return;
    }
    
    impl = new(environments, func);
    Log.Information("impl initialised");
    await impl.Init(handler);
    Log.Information("impl.Init called");
  }

  public static async Task RunFunction(List<FunctionTrigger> triggers) {
    Log.Information("AwsHost RunFunction called");
    if (impl is null) throw new Exception("AwsHost.Init has not been called");

    await impl.RunFunction(triggers);
    Log.Information("impl.RunFunction called");
  }

}

public class AwsHostImpl(List<string> environments, Type func) {

  private AwsHostCentazioEngineAdapter centazio = null!;

  public async Task Init(IAwsFunctionHandler handler) {
    InitLogger();

    var settings = await new SettingsLoader().Load<CentazioSettings>(environments);
    centazio = new(settings, environments, false);
    await centazio.Init([func]);
    Log.Information("Init CentazioEngineAdapter called");
    await InitAwsLambdaFunctionHost(handler);
    Log.Information("InitAwsLambdaFunctionHost called");
  }

  private void InitLogger() {
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .Enrich.WithProperty("FunctionName", func.Name)
        .WriteTo.Console()
        .CreateLogger();
  }

  private async Task InitAwsLambdaFunctionHost(IAwsFunctionHandler handler) {
    using var wrapper = HandlerWrapper.GetHandlerWrapper(handler.Handle);
    using var bootstrap = new LambdaBootstrap(wrapper);
    await bootstrap.RunAsync();
  }

  public async Task<FunctionRunResults> RunFunction(List<FunctionTrigger> triggers) =>
      await centazio.RunFunction(func, triggers);

}