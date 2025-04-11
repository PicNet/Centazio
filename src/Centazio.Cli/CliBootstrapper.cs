using Centazio.Cli;
using Centazio.Cli.Commands;
using Centazio.Cli.Infra.Aws;
using Centazio.Cli.Infra.Az;
using Centazio.Cli.Infra.Gen;
using Centazio.Core;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Host;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

return new CliBootstrapper().Initialise().Start(args);

internal class CliBootstrapper {

  public Cli Initialise() { 
    Environment.SetEnvironmentVariable("IS_CLI", "true");
    
    Log.Logger = LogInitialiser.GetFileAndConsoleConfig().CreateLogger();
    var services = InitialiseDi();
    var cli = services.GetRequiredService<Cli>();
    
    InitialiseExceptionHandler(cli);
    return cli;
  }
  

  private ServiceProvider InitialiseDi() {
    var svcs = new ServiceCollection();
    var available = LoadSettingsAndSecretsIfAvailable();
    svcs
        .AddSingleton<ITypeRegistrar>(new TypeRegistrar(svcs))
        .AddSingleton<InteractiveCliMenuCommand>()
        .AddSingleton<Cli>()
        .AddSingleton<InteractiveMenu>()
        .AddSingleton<CommandsTree>()
        .AddSingleton<ICommandRunner, CommandRunner>()
        .AddSingleton<ITemplater, Templater>()
        .AddSingleton<ICentazioCodeGenerator, CentazioCodeGenerator>()
        
        .AddSingleton<IAwsAccounts, AwsAccounts>()
        .AddSingleton<IAzSubscriptions, AzSubscriptions>()
        .AddSingleton<IAzResourceGroups, AzResourceGroups>()
        .AddSingleton<IAwsFunctionDeployer, AwsFunctionDeployer>()
        .AddSingleton<IAzFunctionDeployer, AzFunctionDeployer>()
        .AddSingleton<IAzFunctionDeleter, AzFunctionDeleter>()
        .AddSingleton<CentazioHost>();
    RegisterCliCommands();
    return svcs.BuildServiceProvider();
    
    bool LoadSettingsAndSecretsIfAvailable() {
      var dir = FsUtils.TryToFindDirectoryOfFile(CentazioConstants.SETTINGS_FILE_NAME);
      if (dir is null) return false;
      var conf = new SettingsLoaderConfig(RootDirectory: dir);  
      var settings = SettingsLoader.RegisterSettingsHierarchy(new SettingsLoader(conf).Load<CentazioSettings>(CentazioConstants.DEFAULT_ENVIRONMENT, "aws", "azure"), svcs);
      svcs.AddSingleton(new SecretsFileLoader(settings.GetSecretsFolder()).Load<CentazioSecrets>(CentazioConstants.DEFAULT_ENVIRONMENT));
      return true;
    }
    
    void RegisterCliCommands() {
      GetType().Assembly.GetTypes()
          .Where(t => !t.IsAbstract && t.IsAssignableTo(typeof(ICentazioCommand)))
          // do not register any command that requires settings or secrets if they are not `available`
          .Where(t => available || !DoesCommandRequireSettings(t))
          .ForEach(t => {
            svcs.AddSingleton<ICentazioCommand>(prov => (ICentazioCommand) prov.GetRequiredService(t));
            svcs.AddSingleton(t);
          });
    }
    
    bool DoesCommandRequireSettings(Type cmdtype) => 
        cmdtype.GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Any(p => typeof(CentazioSettings).IsAssignableFrom(p.ParameterType) 
                || typeof(CentazioSecrets).IsAssignableFrom(p.ParameterType));
  }

  private void InitialiseExceptionHandler(Cli cli) => AppDomain.CurrentDomain.UnhandledException += 
      (_, e) => cli.ReportException((Exception) e.ExceptionObject, e.IsTerminating);

}

