using Centazio.Core.Runner;

namespace Centazio.Cli.Commands.Aws;

public static class AwsCommandsHelpers {
  public static string GetTargetFunction(string assembly) {
    var ass = ReflectionUtils.LoadAssembly(assembly);
    var options = IntegrationsAssemblyInspector.GetCentazioFunctions(ass, []).Select(f => f.Name).ToList();
    if (!options.Any()) throw new Exception($"Assembly '{assembly}' does not contain any Centazio Functions");
    if (options.Count == 1) return options.Single();
    return UiHelpers.Select("Select Function:", options);
  }
}