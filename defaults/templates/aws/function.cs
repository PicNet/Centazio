using Centazio.Core.Runner;
using Amazon.Lambda.Core;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Serilog;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
{{it.assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))}}

namespace {{it.NewAssemblyName}};

public class {{it.ClassName}}Handler
{
    private static readonly Lazy<Task<IRunnableFunction>> impl;

    static {{it.ClassName}}Handler()
    {    
        impl = new(async () => await new FunctionsInitialiser("{{it.Environment}}").Init<{{it.ClassFullName}}>(), 
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>
    /// Lambda function handler for {{it.ClassName}}
    /// </summary>
    /// <param name="input">Lambda event data</param>
    /// <param name="context">Lambda context</param>
    /// <returns>Lambda response</returns>
    public async Task<string> HandleAsync(object input, ILambdaContext context)
    {
        var start = DateTime.Now;
        Log.Information("{{it.ClassName}} running");
        try 
        { 
            await (await impl.Value).RunFunction();
            return $"{{it.ClassName}} executed successfully";
        } 
        finally 
        { 
            Log.Information($"{{it.ClassName}} completed, took {(DateTime.Now - start).TotalSeconds:N0}s");
        }
    }
}