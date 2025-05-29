using System.Reflection;
using Centazio.Cli.Commands;
using Centazio.Core;
using Centazio.Core.Misc;
using Centazio.Core.Secrets;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Cli.Tests.Commands;

public class CheckThatCommandsLoadCorrectSettingsAndSecrets {

  [Test] public void Go() {
    var errors = new List<string>();
    typeof(Cli).Assembly.GetExportedTypes()
        .Where(type => {
          var iscommand = typeof(ICentazioCommand).IsAssignableFrom(type) && type is { IsClass: true, IsAbstract: false };
          var isinfra = type.Namespace!.StartsWith("Centazio.Cli.Infra") && type is { IsClass: true, IsAbstract: false };
          return iscommand || isinfra;
        })
        .ForEach(type => {
          var args = type.GetConstructors().SelectMany(ctor => ctor.GetParameters().Where(ShouldUseKeyedService)).ToList();
          if (!args.Any()) return;
          
          args.ForEach(arg => {
            Console.WriteLine("Type: " + type.Name + " Arg: " + arg.ParameterType.Name);
            var attribute = arg.GetCustomAttributes<FromKeyedServicesAttribute>()?.SingleOrDefault();
            var key = attribute?.Key.ToString();
            if (type.Namespace!.ToLower().Contains($".{CentazioConstants.Hosts.Aws}", StringComparison.OrdinalIgnoreCase)) {
              if (key is not CentazioConstants.Hosts.Aws) errors.Add($"type[{type.FullName}] ctor arg[{arg.Name}] should have [FromKeyedServices(CentazioConstants.Hosts.Aws)]");
            } else if (type.Namespace.ToLower().Contains($".{CentazioConstants.Hosts.Az}", StringComparison.OrdinalIgnoreCase)) {
              if (key is not CentazioConstants.Hosts.Az) errors.Add($"type[{type.FullName}] ctor arg[{arg.Name}] should have [FromKeyedServices(CentazioConstants.Hosts.Az)]");
            } else {
              if (attribute is not null) errors.Add($"type[{type.FullName}] ctor arg[{arg.Name}] should NOT have [FromKeyedServices]");
            }
          });
        });
    Assert.That(errors, Is.Empty, String.Join("\n", errors));
  }
  
  private bool ShouldUseKeyedService(ParameterInfo arg) {
    return (arg.ParameterType.Namespace == "Centazio.Core.Settings" && ReflectionUtils.IsRecord(arg.ParameterType)) ||
        typeof(CentazioSecrets).IsAssignableFrom(arg.ParameterType);
  }

}