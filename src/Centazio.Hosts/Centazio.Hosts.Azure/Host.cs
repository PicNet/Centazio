using Centazio.Core.Misc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker;
using Serilog;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;

namespace Centazio.Hosts.Azure;

public class Host {

  public void Init() {
    var connstr = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
    InitLogger(connstr);
    InitHost(connstr);
  }

  private void InitLogger(string? connstr) {
    var logconfig = LogInitialiser.GetConsoleConfig();
    if (!String.IsNullOrWhiteSpace(connstr)) logconfig = logconfig.WriteTo.ApplicationInsights(connstr, new TraceTelemetryConverter(), Serilog.Events.LogEventLevel.Information);
    Log.Logger = logconfig.CreateLogger();
  }

  private void InitHost(string? connstr) {
    new HostBuilder()
      .UseSerilog()
      .ConfigureFunctionsWorkerDefaults()
      .ConfigureServices((_, services) => {
        if (!String.IsNullOrWhiteSpace(connstr)) InitialiseApplicationInsights(services);
        services.AddLogging(builder => { builder.AddSerilog(dispose: true); });
      })
      .Build().Run();

    void InitialiseApplicationInsights(IServiceCollection services) {
      services.AddApplicationInsightsTelemetryWorkerService(options => options.ConnectionString = connstr);
      services.ConfigureFunctionsApplicationInsights();
    }
  }
}