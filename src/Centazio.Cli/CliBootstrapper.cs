using Centazio.Cli;
using Centazio.Cli.Commands;
using Centazio.Cli.Utils;
using Centazio.Core;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

return await new CliBootstrapper().Initialise().Start(args);

internal class CliBootstrapper {

  public Cli Initialise() { 
    InitialiseLogger();
    var services = InitialiseDi();
    var cli = services.GetRequiredService<Cli>();
    InitialiseExceptionHandler(cli);
    return cli;
  }

  private static void InitialiseLogger() => Log.Logger = new LoggerConfiguration()
      .WriteTo
      .File("log.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
      .MinimumLevel.Debug()
      .CreateLogger();

  private ServiceProvider InitialiseDi() {
    var svcs = new ServiceCollection();
    
    GetType().Assembly.GetTypes()
        .Where(t => !t.IsAbstract && t.IsAssignableTo(typeof(ICentazioCommand)))
        .ForEachIdx(t => svcs.AddSingleton(t));
    
    return svcs
        .AddSingleton<IServiceCollection>(svcs)
        
        .AddSingleton<ISettingsLoader<CliSettings>, SettingsLoader<CliSettings>>()
        .AddSingleton<CliSettings>(provider => provider.GetRequiredService<ISettingsLoader<CliSettings>>().Load())
        
        .AddSingleton<ISecretsLoader<CliSecrets>>(provider => {
          var settings = provider.GetRequiredService<CliSettings>();
          return new NetworkLocationEnvFileSecretsLoader<CliSecrets>(settings.SecretsFolder, "dev");
        }) 
        .AddSingleton<CliSecrets>(provider => provider.GetRequiredService<ISecretsLoader<CliSecrets>>().Load())
        
        .AddSingleton<ICliSplash, CliSplash>()
        .AddSingleton<Cli>()
        .AddSingleton<IInteractiveMenu, InteractiveMenu>()
        .AddSingleton<ICommandTree, CommandTree>()
        .BuildServiceProvider();
  }

  private void InitialiseExceptionHandler(Cli cli) => AppDomain.CurrentDomain.UnhandledException += 
      (_, e) => cli.ReportException((Exception) e.ExceptionObject, e.IsTerminating);

}

