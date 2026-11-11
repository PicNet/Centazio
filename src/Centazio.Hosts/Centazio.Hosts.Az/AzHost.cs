using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker;
using Serilog;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;

namespace Centazio.Hosts.Az;

/// <summary>
/// Created once by the generated function app `Program`, which owns it.  `Init` registers this instance in the
/// Functions worker services so the generated function classes receive it by injection.
/// </summary>
public class AzHost(List<string> environments, List<Type> functions) {

  private readonly string? APP_INSIGHTS_CONN_STR = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")?.Trim();
  private readonly AzHostCentazioEngineAdapter centazio = new(environments);

  public async Task Init() {
    InitLogger();
    await centazio.Init(functions);
    
    InitAzureFunctionAppHost();
    
  }

  private void InitLogger() => Log.Logger = 
      LogInitialiser.GetConsoleConfig().InitialiseAppInsightsLogger(APP_INSIGHTS_CONN_STR).CreateLogger();

  private void InitAzureFunctionAppHost() => new HostBuilder()
      .UseSerilog()
      .ConfigureFunctionsWorkerDefaults()
      .ConfigureServices((_, svcs) => svcs
          .AddSingleton(this)
          .InitialiseAppInsights(APP_INSIGHTS_CONN_STR)
          .AddLogging(builder => builder.AddSerilog(dispose: true)))
      .Build()
      .Run();
  

  public async Task RunFunction(Type type, List<FunctionTrigger> triggers) => await centazio.RunFunction(type, triggers);

}

public static class AzHostHelperExtensionMethods {
  public static LoggerConfiguration InitialiseAppInsightsLogger(this LoggerConfiguration cfg, string? connstr) {
    if (String.IsNullOrWhiteSpace(connstr)) return cfg;
    return cfg.WriteTo.ApplicationInsights(connstr, new TraceTelemetryConverter(), Serilog.Events.LogEventLevel.Information);
  }
  
  public static IServiceCollection InitialiseAppInsights(this IServiceCollection svcs, string? connstr) {
    if (String.IsNullOrWhiteSpace(connstr)) return svcs;
    svcs.AddApplicationInsightsTelemetryWorkerService(options => options.ConnectionString = connstr);
    svcs.ConfigureFunctionsApplicationInsights();
    return svcs;
  }
}