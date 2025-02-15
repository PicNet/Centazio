using Centazio.Core.Misc;
using Microsoft.Extensions.Hosting;
using Serilog;

Log.Logger = LogInitialiser.GetConsoleConfig().CreateLogger();

new HostBuilder()
  .ConfigureFunctionsWorkerDefaults()  
  .Build().Run();
