using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker;
using Serilog;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;

namespace Centazio.Hosts.Az;

public static class AzHost {

  private static AzHostImpl? impl;

  public static async Task Init(List<string> environments, List<Type> functions) {
    if (impl is not null) throw new Exception("AzHost.Init already called");
    impl = new(environments, functions);
    await impl.Init();
  }

  public static async Task RunFunction(Type type, List<FunctionTrigger> triggers) {
    if (impl is null) throw new Exception("AzHost.Init has not been called");
    await impl.RunFunction(type, triggers);
  }

}

public class AzHostImpl(List<string> environments, List<Type> functions) {

  private readonly string? APP_INSIGHTS_CONN_STR = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")?.Trim();
  private readonly AzHostInitialiser centazio = new(environments);

  public async Task Init() {
    InitLogger();
    await centazio.Init(functions);
    
    InitAzureHost();
    
  }

  private void InitLogger() => Log.Logger = 
      LogInitialiser.GetConsoleConfig().InitialiseAppInsightsLogger(APP_INSIGHTS_CONN_STR).CreateLogger();

  private void InitAzureHost() => new HostBuilder()
      .UseSerilog()
      .ConfigureFunctionsWorkerDefaults()
      .ConfigureServices((_, svcs) => svcs
          .InitialiseAppInsights(APP_INSIGHTS_CONN_STR)
          .AddLogging(builder => builder.AddSerilog(dispose: true)))
      .Build()
      .Run();
  

  public async Task RunFunction(Type type, List<FunctionTrigger> triggers) => 
      await centazio.GetRunner().RunFunction(centazio.GetFunction(type), triggers);

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