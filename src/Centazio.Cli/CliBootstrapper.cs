using Centazio.Cli;
using Centazio.Cli.Commands;
using Centazio.Cli.Infra.Aws;
using Centazio.Cli.Infra.Az;
using Centazio.Cli.Infra.Gen;
using Centazio.Core;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Providers.Aws.Secrets;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

return (await new CliBootstrapper().Initialise()).Start(args);

internal class CliBootstrapper {

  public async Task<Cli> Initialise() { 
    Environment.SetEnvironmentVariable("IS_CLI", "true");
    
    Log.Logger = LogInitialiser.GetFileAndConsoleConfig().CreateLogger();
    var services = await InitialiseDi();
    var cli = services.GetRequiredService<Cli>();
    
    InitialiseExceptionHandler(cli);
    return cli;
  }
  

  private async Task<ServiceProvider> InitialiseDi() {
    var svcs = new ServiceCollection();
    var indev = await LoadDevSettingsIfAvailableOtherwiseDefaults();
    
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
        .AddSingleton<Centazio.Hosts.Self.SelfHost>();
    if (indev) { devdeps.ForEach(kvp => svcs.AddSingleton(kvp.Key, kvp.Value)); }
    RegisterCliCommands();
    return svcs.BuildServiceProvider();

    async Task<bool> LoadDevSettingsIfAvailableOtherwiseDefaults() {
      var devdir = FsUtils.FindFileDirectory(CentazioConstants.SETTINGS_FILE_NAME);
      var isindev = devdir is not null;
      
      var dir = devdir ?? FsUtils.GetDefaultsDir() ?? throw new Exception("could not find a dev directory or the cli defaults directory");
      var conf = new SettingsLoaderConfig(dir, isindev ? EDefaultSettingsMode.BOTH : EDefaultSettingsMode.ONLY_DEFAULT_SETTINGS);  
      var settings = SettingsLoader.RegisterSettingsHierarchy(await new SettingsLoader(conf).Load<CentazioSettings>(CentazioConstants.DEFAULT_ENVIRONMENT, "aws", "azure"), svcs);
      
      if (isindev) {
        var factory = new SecretsLoaderFactory();
        
        // Register providers based on configuration
        var provider = Enum.Parse<Provider>(settings.SecretsLoaderSettings.Provider ?? "File");
        
        // Register appropriate provider
        switch (provider) {
          case Provider.Aws:
            factory.RegisterAwsProvider();
            break;
          case Provider.File:
          default:
            // Fall back to file loader if no provider specified or using file
            factory.RegisterProvider(Provider.File, s => new SecretsFileLoader(s.GetSecretsFolder()));
            break;
        }

        svcs.AddSingleton<ISecretsLoaderFactory>(factory);
        var loader = factory.CreateSecretsLoader(provider, settings);
        var secrets = await loader.Load<CentazioSecrets>(CentazioConstants.DEFAULT_ENVIRONMENT);
        svcs.AddSingleton(secrets);
      }

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

