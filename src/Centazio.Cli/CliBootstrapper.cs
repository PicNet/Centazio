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
    var indev = LoadDevSettingsIfAvailableOtherwiseDefaults();
    
    // these are dependencies that require dev settings or secrets (with aws/azure details), and are not added
    //    when running in directories without settings.json available
    var devdeps = new Dictionary<Type, Type> {
      { typeof(IAwsAccounts), typeof(AwsAccounts) },
      { typeof(IAzSubscriptions), typeof(AzSubscriptions) },
      { typeof(IAzResourceGroups), typeof(AzResourceGroups) },
      { typeof(IAwsFunctionDeployer), typeof(AwsFunctionDeployer) },
      { typeof(IAzFunctionDeployer), typeof(AzFunctionDeployer) },
      { typeof(IAzFunctionDeleter), typeof(AzFunctionDeleter) },
    };
    svcs
        .AddSingleton<ITypeRegistrar>(new TypeRegistrar(svcs))
        .AddSingleton<Cli>()
        .AddSingleton<InteractiveMenu>()
        .AddSingleton<InteractiveCliMenuCommand>()
        .AddSingleton<CommandsTree>()
        .AddSingleton<ICommandRunner, CommandRunner>()
        .AddSingleton<ITemplater, Templater>()
        .AddSingleton<ICentazioCodeGenerator, CentazioCodeGenerator>()
        .AddSingleton<CentazioHost>();
    if (indev) { devdeps.ForEach(kvp => svcs.AddSingleton(kvp.Key, kvp.Value)); }
    RegisterCliCommands();
    return svcs.BuildServiceProvider();
    
    bool LoadDevSettingsIfAvailableOtherwiseDefaults() {
      var devdir = FsUtils.TryToFindDirectoryOfFile(CentazioConstants.SETTINGS_FILE_NAME);
      var isindev = devdir is not null;
      
      var dir = devdir ?? FsUtils.GetDefaultsDir() ?? throw new Exception("could not find a dev directory or the cli defaults directory");
      var conf = new SettingsLoaderConfig(dir, isindev ? EDefaultSettingsMode.BOTH : EDefaultSettingsMode.ONLY_DEFAULT_SETTINGS);  
      var settings = SettingsLoader.RegisterSettingsHierarchy(new SettingsLoader(conf).Load<CentazioSettings>(CentazioConstants.DEFAULT_ENVIRONMENT, "aws", "azure"), svcs);
      if (isindev) svcs.AddSingleton(new SecretsFileLoader(settings.GetSecretsFolder()).Load<CentazioSecrets>(CentazioConstants.DEFAULT_ENVIRONMENT));
      return isindev;
    }
    
    void RegisterCliCommands() {
      GetType().Assembly.GetTypes()
          .Where(t => !t.IsAbstract && t.IsAssignableTo(typeof(ICentazioCommand)))
          // do not register any command that requires settings or secrets if they are not `available`
          .Where(t => indev || !DoesClassRequireSettings(t))
          .ForEach(t => {
            svcs.AddSingleton<ICentazioCommand>(prov => (ICentazioCommand) prov.GetRequiredService(t));
            svcs.AddSingleton(t);
          });
    }
    
    bool DoesClassRequireSettings(Type cls) {
      var ptypes = cls.GetConstructors()
          .SelectMany(c => c.GetParameters().Select(p => p.ParameterType))
          .ToList();
      return ptypes.Any(t => typeof(CentazioSettings).IsAssignableFrom(t) || typeof(CentazioSecrets).IsAssignableFrom(t) || devdeps.ContainsKey(t));
    }
  }

  private void InitialiseExceptionHandler(Cli cli) => AppDomain.CurrentDomain.UnhandledException += 
      (_, e) => cli.ReportException((Exception) e.ExceptionObject, e.IsTerminating);

}

