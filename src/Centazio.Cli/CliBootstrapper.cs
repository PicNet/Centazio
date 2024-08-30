using Centazio.Cli;
using Centazio.Cli.Commands;
using Centazio.Cli.Infra;
using Centazio.Cli.Infra.Aws;
using Centazio.Cli.Infra.Az;
using Centazio.Core;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Spectre.Console.Cli;

return new CliBootstrapper().Initialise().Start(args);

internal class CliBootstrapper {

  public Cli Initialise() { 
    InitialiseLogger();
    var services = InitialiseDi();
    var cli = services.GetRequiredService<Cli>();
    InitialiseExceptionHandler(cli);
    return cli;
  }

  private static void InitialiseLogger() => Log.Logger = LogInitialiser.GetBaseConfig()
      .WriteTo.File(LogInitialiser.Formatter, "log.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
      .MinimumLevel.Debug()
      .CreateLogger();

  private ServiceProvider InitialiseDi() {
    var svcs = new ServiceCollection();
    
    GetType().Assembly.GetTypes()
        .Where(t => !t.IsAbstract && t.IsAssignableTo(typeof(ICentazioCommand)))
        .ForEachIdx(t => {
          svcs.AddSingleton<ICentazioCommand>(prov => (ICentazioCommand) prov.GetRequiredService(t));
          svcs.AddSingleton(t);
        });
    
    return svcs
        .AddSingleton<ITypeRegistrar>(new TypeRegistrar(svcs))
        .AddSingleton<InteractiveCliMeneCommand>()
        .AddSingleton<ISettingsLoader<CentazioSettings>, SettingsLoader<CentazioSettings>>()
        .AddSingleton<CentazioSettings>(provider => provider.GetRequiredService<ISettingsLoader<CentazioSettings>>().Load())
        
        .AddSingleton<ISecretsLoader<CentazioSecrets>>(provider => {
          var settings = provider.GetRequiredService<CentazioSettings>();
          return new NetworkLocationEnvFileSecretsLoader<CentazioSecrets>(settings.SecretsFolder, "dev");
        }) 
        .AddSingleton<CentazioSecrets>(provider => provider.GetRequiredService<ISecretsLoader<CentazioSecrets>>().Load())
        
        .AddSingleton<Cli>()
        .AddSingleton<InteractiveMenu>()
        .AddSingleton<CommandTree>()
        .AddSingleton<IAwsAccounts, AwsAccounts>()
        .AddSingleton<IAzSubscriptions, AzSubscriptions>()
        .AddSingleton<IAzResourceGroups, AzResourceGroups>()
        .BuildServiceProvider();
  }

  private void InitialiseExceptionHandler(Cli cli) => AppDomain.CurrentDomain.UnhandledException += 
      (_, e) => cli.ReportException((Exception) e.ExceptionObject, e.IsTerminating);

}

