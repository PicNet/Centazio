using System.Reflection;
using Centazio.Cli.Commands;
using Centazio.Cli.Commands.Dev;
using Centazio.Core;
using Centazio.Core.Misc;
using Centazio.Core.Secrets;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Cli.Tests.Commands;

public class CheckThatCommandsLoadCorrectSettingsAndSecrets {

  [Test] public void Check_settings_useage_in_ctor() {
    var errors = new List<string>();
    typeof(Cli).Assembly.GetExportedTypes()
        .Where(IsTestableType)
        .ForEach(type => {
          type.GetConstructors()
              .SelectMany(ctor => ctor.GetParameters().Where(IsSettingsArgument))
              .ForEach(CheckArg(type));
        });
    Assert.That(errors, Is.Empty, String.Join("\n", errors));

    bool IsSettingsArgument(ParameterInfo arg) => 
        arg.ParameterType.Namespace == "Centazio.Core.Settings" && ReflectionUtils.IsRecord(arg.ParameterType);

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
  
  [Test] public void Check_secrets_usage_in_ctor() {
    var errors = new List<string>();
    typeof(Cli).Assembly.GetExportedTypes()
        .Where(IsTestableType)
        .ForEach(type => {
          type.GetConstructors()
              .SelectMany(ctor => ctor.GetParameters().Where(IsSecretsArgument))
              .ForEach(CheckArg(type));
        });
    Assert.That(errors, Is.Empty, String.Join("\n", errors));
    
    bool IsSecretsArgument(ParameterInfo arg) => typeof(CentazioSecrets).IsAssignableFrom(arg.ParameterType);
    
    Action<ParameterInfo> CheckArg(Type type) => 
        arg => 
            errors.Add($"type[{type.FullName}] ctor arg[{arg.Name}] is a Secrets argument.  The CLI should only use `ICliSecretsManager` not the secrets type directly.");
  }
  
  private bool IsTestableType(Type type) {
      var iscommand = typeof(ICentazioCommand).IsAssignableFrom(type) && type is { IsClass: true, IsAbstract: false };
      var isinfra = type.Namespace!.StartsWith("Centazio.Cli.Infra") && type is { IsClass: true, IsAbstract: false };
      return iscommand || isinfra;
    }
  
}