using Centazio.Cli;
using Centazio.Cli.Utils;
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

  private ServiceProvider InitialiseDi() => new ServiceCollection()
      .AddSingleton<ICliSplash, CliSplash>()
      .AddSingleton<Cli>()
      .BuildServiceProvider();

  private void InitialiseExceptionHandler(Cli cli) => AppDomain.CurrentDomain.UnhandledException += 
      (_, e) => cli.ReportException((Exception) e.ExceptionObject, e.IsTerminating);

}

