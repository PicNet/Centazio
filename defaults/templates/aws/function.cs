using Centazio.Core.Runner;
using Amazon.Lambda.Core;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Serilog;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace [NewAssemblyName];

public class [ClassName]Handler
{
    private static readonly Lazy<Task<IRunnableFunction>> impl;

    static [ClassName]Handler()
    {    
        impl = new(async () => await new FunctionsInitialiser("[Environment]").Init<[ClassFullName]>(), 
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>
    /// Lambda function handler for [ClassName]
    /// </summary>
    /// <param name="input">Lambda event data</param>
    /// <param name="context">Lambda context</param>
    /// <returns>Lambda response</returns>
    public async Task<string> HandleAsync(object input, ILambdaContext context)
    {
        var start = DateTime.Now;
        Log.Information("[ClassName] running");
        try 
        { 
            await (await impl.Value).RunFunction();
            return $"[ClassName] executed successfully";
        } 
        finally 
        { 
            Log.Information($"[ClassName] completed, took {(DateTime.Now - start).TotalSeconds:N0}s");
        }
    }
}