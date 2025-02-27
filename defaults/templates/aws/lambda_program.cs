using Centazio.Core.Misc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;

namespace [NewAssemblyName];

public class Program
{
    /// <summary>
    /// The main entry point for the Lambda function.
    /// </summary>
    public static async Task Main()
    {
        // Configure logging
        Log.Logger = LogInitialiser.GetConsoleConfig().CreateLogger();
        
        // Register Serilog
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddSerilog(Log.Logger));
        var serviceProvider = services.BuildServiceProvider();
        
        // Build Lambda bootstrap
        using var handlerWrapper = new [ClassName]Handler();
        using var bootstrap = new LambdaBootstrap(handlerWrapper.HandleAsync, new DefaultLambdaJsonSerializer());
        
        await bootstrap.RunAsync();
    }
}