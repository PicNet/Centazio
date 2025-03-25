using Centazio.Core.Misc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;

var connectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");

Log.Logger = new LoggerConfiguration()
  .MinimumLevel.Debug()
  .WriteTo.Console()
  .WriteTo.ApplicationInsights(
    connectionString,
    new TraceTelemetryConverter(),
    Serilog.Events.LogEventLevel.Information)
  .CreateLogger();

new HostBuilder()
  .UseSerilog()
  .ConfigureFunctionsWorkerDefaults()
  .ConfigureServices((context, services) => {
    services.AddApplicationInsightsTelemetryWorkerService(options => {
      if (!string.IsNullOrWhiteSpace(connectionString))
        options.ConnectionString = connectionString;
    });

    services.ConfigureFunctionsApplicationInsights();

    services.Configure<LoggerFilterOptions>(options => {
      options.Rules.Clear();
      options.Rules.Add(new LoggerFilterRule(
        "Microsoft.ApplicationInsights.ApplicationInsightsLoggerProvider",
        null,
        LogLevel.Information,
        null));
    });

    services.AddLogging(loggingBuilder => {
      loggingBuilder.AddSerilog(dispose: true);
    });
  })
  .Build()
  .Run();
