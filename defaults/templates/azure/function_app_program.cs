using Centazio.Core.Misc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

new HostBuilder()
  .ConfigureFunctionsWorkerDefaults()  
  .ConfigureServices(services => services.AddLogging(builder =>
        builder.AddSerilog(Log.Logger = LogInitialiser.GetConsoleConfig().CreateLogger())))
  .Build().Run();
