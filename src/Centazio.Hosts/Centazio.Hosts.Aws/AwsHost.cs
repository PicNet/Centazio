using Amazon.CloudWatchLogs;
using Amazon.Lambda.RuntimeSupport;
using Centazio.Core.Runner;
using Centazio.Core.Settings;
using Serilog;
using Serilog.Formatting.Json;
using Serilog.Sinks.AwsCloudWatch;
using Type = System.Type;

namespace Centazio.Hosts.Aws;

public static class AwsHost {

  private static AwsHostImpl? impl;

  public static async Task Init(List<string> environments, IAwsFunctionHandler handler, Type func) {
    if (impl is not null) throw new Exception("AwsHost.Init already called");

    impl = new(environments, func);
    await impl.Init(handler);
  }

  public static async Task RunFunction(List<FunctionTrigger> triggers) {
    if (impl is null) throw new Exception("AwsHost.Init has not been called");

    await impl.RunFunction(triggers);
  }

}

public class AwsHostImpl(List<string> environments, Type func) {

  private AwsHostCentazioEngineAdapter centazio = null!;

  public async Task Init(IAwsFunctionHandler handler) {
    InitLogger();

    var settings = await new SettingsLoader().Load<CentazioSettings>(environments);
    centazio = new(settings, environments, false);
    await centazio.Init([func]);
    await InitAwsLambdaFunctionHost(handler);
  }

  private void InitLogger() {
    var options = new CloudWatchSinkOptions {
      LogGroupName = $"/aws/lambda/{func.Name.ToLower()}",
      TextFormatter = new JsonFormatter(),
      MinimumLogEventLevel = Serilog.Events.LogEventLevel.Information,
      CreateLogGroup = true,
      LogStreamNameProvider = new DefaultLogStreamProvider()
    };

    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.AmazonCloudWatch(options, new AmazonCloudWatchLogsClient())
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