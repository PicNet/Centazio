using Centazio.Cli.Infra;
using Centazio.Core.Misc;
using net.r_eg.MvsSln.Core;

namespace Centazio.Cli.Commands.Gen;

internal class AzureCloudSolutionGenerator(FunctionProjectMeta project, string environment) : CloudSolutionGenerator(project, environment) {

  protected override AbstractCloudProjectGenerator GetCloudProjectGenerator(IXProject proj) => new AzureCloudProjectGenerator(project, proj, environment);

  internal class AzureCloudProjectGenerator(FunctionProjectMeta projmeta, IXProject slnproj, string environment) : AbstractCloudProjectGenerator(projmeta, slnproj, environment) {

    protected override async Task AddCloudSpecificContentToProject(List<Type> functions) {
      await AddAzureNuGetReferencesToProject();
      await AddAzHostJsonFileToProject();
      await AddAzureFunctionsToProject(functions);
    }
  
    private Task AddAzureNuGetReferencesToProject() => 
        AddLatestNuGetReferencesToProject([
          "Microsoft.Azure.Functions.Worker",
          "Microsoft.Azure.Functions.Worker.Extensions.Timer",
          "Microsoft.Azure.Functions.Worker.Sdk"
        ]);

    // todo: read from external template file
    private async Task AddAzHostJsonFileToProject() {
      slnproj.AddItem("None", "host.json", [new("CopyToOutputDirectory", "PreserveNewest")]);
      var contents = @"{
    ""version"": ""2.0"",
    ""logging"": {
        ""applicationInsights"": {
            ""samplingSettings"": {
                ""isEnabled"": true,
                ""excludedTypes"": ""Request""
            },
            ""enableLiveMetricsFilters"": true
        }
    }
}";
      await File.WriteAllTextAsync(Path.Combine(slnproj.ProjectPath, $"host.json"), contents);
    }
  
    private async Task AddAzureFunctionsToProject(List<Type> functions) {
      // todo: these templates should be in other files to allow users to change the template if required
      await functions.ForEachSequentialAsync(async func => {
        var clcontent = @"
using Microsoft.Azure.Functions.Worker;
using Centazio.Core.Runner;

namespace {{NewAssemblyName}};

public class {{ClassName}}Azure {  
  private static readonly Lazy<Task<IRunnableFunction>> impl;

  static {{ClassName}}Azure() {    
    impl = new(async () => await new FunctionsInitialiser().Init<{{ClassName}}>(), LazyThreadSafetyMode.ExecutionAndPublication);
  }

  [Function(nameof({{ClassName}}))] public async Task Run([TimerTrigger(""*/10 * * * * *"")] TimerInfo timer) {    
    await (await impl.Value).RunFunction(); 
  }
}"
            .Replace("{{ClassName}}", func.Name)
            .Replace("{{FunctionNamespace}}", func.Namespace)
            .Replace("{{NewAssemblyName}}", slnproj.ProjectName);
      
        await File.WriteAllTextAsync(Path.Combine(slnproj.ProjectPath, $"{func.Name}.cs"), clcontent);
        await File.WriteAllTextAsync(Path.Combine(slnproj.ProjectPath, $"Program.cs"), @"
using Centazio.Core.Misc;
using Microsoft.Extensions.Hosting;
using Serilog;

Log.Logger = LogInitialiser.GetConsoleConfig().CreateLogger();

new HostBuilder()
  .ConfigureFunctionsWorkerDefaults()  
  .Build().Run();
");
      });
    }

  }

}