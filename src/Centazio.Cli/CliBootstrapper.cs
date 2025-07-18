﻿using Centazio.Cli;
using Centazio.Cli.Commands;
using Centazio.Cli.Infra.Aws;
using Centazio.Cli.Infra.Az;
using Centazio.Cli.Infra.Gen;
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
        .AddSingleton<Centazio.Hosts.Self.SelfHost>()
        .AddSingleton<ICliSecretsManager>(prov => new CliSecretsManager(prov));
    if (indev) { devdeps.ForEach(kvp => svcs.AddSingleton(kvp.Key, kvp.Value)); }
    RegisterCliCommands();
    return svcs.BuildServiceProvider();

    async Task<bool> LoadDevSettingsIfAvailableOtherwiseDefaults() {
      var devdir = FsUtils.FindFileDirectory(CentazioConstants.SETTINGS_FILE_NAME);
      var isindev = devdir is not null;
      
      var dir = devdir ?? FsUtils.GetDefaultsDir() ?? throw new Exception("could not find a dev directory or the cli defaults directory");
      var conf = new SettingsLoaderConfig(dir, isindev ? EDefaultSettingsMode.BOTH : EDefaultSettingsMode.ONLY_DEFAULT_SETTINGS);
      
      SettingsLoader.RegisterSettingsHierarchy(await new SettingsLoader(conf).Load<CentazioSettings>(CentazioConstants.DEFAULT_ENVIRONMENT), svcs, String.Empty);
      SettingsLoader.RegisterSettingsHierarchy(await new SettingsLoader(conf).Load<CentazioSettings>(CentazioConstants.DEFAULT_ENVIRONMENT, CentazioConstants.Hosts.Aws), svcs, CentazioConstants.Hosts.Aws);
      SettingsLoader.RegisterSettingsHierarchy(await new SettingsLoader(conf).Load<CentazioSettings>(CentazioConstants.DEFAULT_ENVIRONMENT, CentazioConstants.Hosts.Az), svcs, CentazioConstants.Hosts.Az);
      
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
      
      bool DoesClassRequireSettings(Type cls) => 
          cls.GetConstructors()
              .SelectMany(c => c.GetParameters().Select(p => p.ParameterType))
              .Any(t => typeof(CentazioSettings).IsAssignableFrom(t) || devdeps.ContainsKey(t));
    }
  }

  private void InitialiseExceptionHandler(Cli cli) => AppDomain.CurrentDomain.UnhandledException += 
      (_, e) => cli.ReportException((Exception) e.ExceptionObject, e.IsTerminating);

}

