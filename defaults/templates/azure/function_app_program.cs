using Centazio.Core.Misc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;
using System;

var connstr = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
var logconfig = LogInitialiser.GetConsoleConfig();

if (!string.IsNullOrWhiteSpace(connstr)) {
  logconfig = logconfig.WriteTo.ApplicationInsights(
    connstr,
    new TraceTelemetryConverter(),
    Serilog.Events.LogEventLevel.Information
  );
}

Log.Logger = logconfig.CreateLogger();

new HostBuilder()
  .UseSerilog()
  .ConfigureFunctionsWorkerDefaults()
  .ConfigureServices((context, services) => {
    if(!string.IsNullOrWhiteSpace(connstr)) InitialiseApplicationInsights(services, connstr);

    services.AddLogging(builder => {
      builder.AddSerilog(dispose: true);
    });
  })
  .Build().Run();

void InitialiseApplicationInsights(IServiceCollection services, string connstr) {
  services.AddApplicationInsightsTelemetryWorkerService(options => {
    options.ConnectionString = connstr;
  });

  services.ConfigureFunctionsApplicationInsights();
}
