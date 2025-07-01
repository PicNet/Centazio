using System.Reflection;
using Centazio.Cli.Commands;
using Centazio.Cli.Commands.Dev;
using Centazio.Core;
using Centazio.Core.Misc;
using Centazio.Core.Secrets;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Cli.Tests.Commands;

// todo: change to instead check that CentazioSecrets is loaded using `ICliSecretsManager`
public class CheckThatCommandsLoadCorrectSettingsAndSecrets {

  [Test] public void Go() {
    var errors = new List<string>();
    typeof(Cli).Assembly.GetExportedTypes()
        .Where(IsTestableType)
        .ForEach(type => {
          type.GetConstructors()
              .SelectMany(ctor => ctor.GetParameters().Where(ShouldUseKeyedService))
              .ForEach(CheckArg(type));
        });
    Assert.That(errors, Is.Empty, String.Join("\n", errors));

    bool IsTestableType(Type type) {
      var iscommand = typeof(ICentazioCommand).IsAssignableFrom(type) && type is { IsClass: true, IsAbstract: false };
      var isinfra = type.Namespace!.StartsWith("Centazio.Cli.Infra") && type is { IsClass: true, IsAbstract: false };
      return iscommand || isinfra;
    }

    Action<ParameterInfo> CheckArg(Type type) {
      return arg => {
        var attribute = arg.GetCustomAttributes<FromKeyedServicesAttribute>().SingleOrDefault();
        var key = attribute?.Key.ToString();
        if (type.Namespace!.ToLower().Contains($".{CentazioConstants.Hosts.Aws}", StringComparison.OrdinalIgnoreCase)) {
          if (key is not CentazioConstants.Hosts.Aws) errors.Add($"type[{type.FullName}] ctor arg[{arg.Name}] should have [FromKeyedServices(CentazioConstants.Hosts.Aws)]");
        } else if (type.Namespace.ToLower().Contains($".{CentazioConstants.Hosts.Az}", StringComparison.OrdinalIgnoreCase)) {
          if (key is not CentazioConstants.Hosts.Az) errors.Add($"type[{type.FullName}] ctor arg[{arg.Name}] should have [FromKeyedServices(CentazioConstants.Hosts.Az)]");
        } else if (type.Namespace != typeof(PackageAndPublishNuGetsCommand).Namespace) { // alow Dev namespace to have any required `FromKeyedServices`
          if (attribute is not null) errors.Add($"type[{type.FullName}] ctor arg[{arg.Name}] should NOT have [FromKeyedServices]");
        }
      };
    }
  }
  
  private bool ShouldUseKeyedService(ParameterInfo arg) {
    return (arg.ParameterType.Namespace == "Centazio.Core.Settings" && ReflectionUtils.IsRecord(arg.ParameterType)) ||
        typeof(CentazioSecrets).IsAssignableFrom(arg.ParameterType);
  }

}