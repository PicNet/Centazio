using Amazon.Lambda.RuntimeSupport;
using Centazio.Core.Runner;
using Centazio.Core.Settings;
using Serilog;
using Type = System.Type;

namespace Centazio.Hosts.Aws;

/// <summary>
/// Created once by the generated lambda `Program`, which owns it and passes it to the function handler.  `Init` 
/// initialises the engine and then runs the lambda bootstrap, which calls the handler for each invocation.
/// </summary>
public class AwsHost(List<string> environments, Type func) {

  private AwsHostCentazioEngineAdapter? centazio;

  public async Task Init(IAwsFunctionHandler handler) {
    if (centazio is not null) throw new Exception("AwsHost.Init already called");
    InitLogger();

    var settings = await new SettingsLoader().Load<CentazioSettings>(environments);
    centazio = CreateEngine(settings, environments);
    await centazio.Init([func]);
    await InitAwsLambdaFunctionHost(handler);
    Log.Information("InitAwsLambdaFunctionHost called");
  }

  // lambda logs function exceptions and returns an error result
  public static AwsHostCentazioEngineAdapter CreateEngine(CentazioSettings settings, List<string> environments) => new(settings, environments, false);

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

  public async Task<FunctionRunResults> RunFunction(List<FunctionTrigger> triggers) {
    if (centazio is null) throw new Exception("AwsHost.Init has not been called");
    return await centazio.RunFunction(func, triggers);
  }

}