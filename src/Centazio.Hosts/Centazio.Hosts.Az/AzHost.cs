using Centazio.Core.Misc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker;
using Serilog;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;

namespace Centazio.Hosts.Az;

public class AzHost {

  private string? APP_INSIGHTS_CONN_STR => Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")?.Trim();
  
  public void Init() {
    InitLogger();
    InitHost();
  }

  private void InitLogger() => Log.Logger = 
      LogInitialiser.GetConsoleConfig().InitialiseAppInsightsLogger(APP_INSIGHTS_CONN_STR).CreateLogger();

  private void InitHost() => new HostBuilder()
      .UseSerilog()
      .ConfigureFunctionsWorkerDefaults()
      .ConfigureServices((_, svcs) => svcs
          .InitialiseAppInsights(APP_INSIGHTS_CONN_STR)
          .AddLogging(builder => builder.AddSerilog(dispose: true)))
      .Build().Run();

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