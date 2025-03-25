using Centazio.Core.Misc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.ApplicationInsights.WorkerService;
using Microsoft.Azure.Functions.Worker;
using Serilog;

new HostBuilder()
  .UseSerilog()
  .ConfigureFunctionsWorkerDefaults()
  .ConfigureServices((context, services) => {
    var connectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");

    services.AddApplicationInsightsTelemetryWorkerService(options => {
      if (!string.IsNullOrWhiteSpace(connectionString)) options.ConnectionString = connectionString;
    });

    services.ConfigureFunctionsApplicationInsights();

    services.AddLogging(loggingBuilder => {
      loggingBuilder.AddSerilog(dispose: true);
    });
  }).Build().Run();
