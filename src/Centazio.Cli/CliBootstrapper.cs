using Centazio.Cli;
using Centazio.Cli.Commands;
using Centazio.Cli.Commands.Az;
using Centazio.Cli.Infra;
using Centazio.Cli.Infra.Aws;
using Centazio.Cli.Infra.Az;
using Centazio.Core.Misc;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Host;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Spectre.Console.Cli;

return new CliBootstrapper().Initialise().Start(args);

internal class CliBootstrapper {

  public Cli Initialise() { 
    Log.Logger = LogInitialiser.GetFileAndConsoleConfig().CreateLogger();
    var services = InitialiseDi();
    var cli = services.GetRequiredService<Cli>();
    
    InitialiseExceptionHandler(cli);
    return cli;
  }
  

  private ServiceProvider InitialiseDi() {
    var svcs = new ServiceCollection();
    
    GetType().Assembly.GetTypes()
        .Where(t => !t.IsAbstract && t.IsAssignableTo(typeof(ICentazioCommand)))
        .ForEach(t => {
          svcs.AddSingleton<ICentazioCommand>(prov => (ICentazioCommand) prov.GetRequiredService(t));
          svcs.AddSingleton(t);
        });
    
    var settings = SettingsLoader.RegisterSettingsHierarchy(new SettingsLoader().Load<CentazioSettings>("dev"), svcs);
    return svcs
        .AddSingleton<ITypeRegistrar>(new TypeRegistrar(svcs))
        .AddSingleton<InteractiveCliMeneCommand>()
        .AddSingleton(new NetworkLocationEnvFileSecretsLoader(settings.GetSecretsFolder()).Load<CentazioSecrets>("dev"))
        
        .AddSingleton<Cli>()
        .AddSingleton<InteractiveMenu>()
        .AddSingleton<CommandsTree>()
        .AddSingleton<IAwsAccounts, AwsAccounts>()
        .AddSingleton<IAzSubscriptions, AzSubscriptions>()
        .AddSingleton<IAzResourceGroups, AzResourceGroups>()
        .AddSingleton<IAzFunctionDeployer, AzFunctionDeployer>()
        .AddSingleton<CentazioHost>()
        .BuildServiceProvider();
  }

  private void InitialiseExceptionHandler(Cli cli) => AppDomain.CurrentDomain.UnhandledException += 
      (_, e) => cli.ReportException((Exception) e.ExceptionObject, e.IsTerminating);

}

