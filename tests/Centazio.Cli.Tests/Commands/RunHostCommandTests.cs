using Centazio.Cli.Commands.Host;
using Centazio.Core;
using Centazio.Hosts.Self;

namespace Centazio.Cli.Tests.Commands;

public class RunHostCommandTests {

  [Test] public async Task Test_command_runs_for_2_seconds_without_errors() {
    var cmd = new RunHostCommand(await F.Settings(), new SelfHost(new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token));
    await cmd.ExecuteImpl(new RunHostCommand.Settings { AssemblyNames = "Centazio.TestFunctions", Environments = [CentazioConstants.DEFAULT_ENVIRONMENT] });
  }
  
  [Test] public async Task Test_command_runs_for_2_seconds_without_errors_AppSheet() {
    var cmd = new RunHostCommand(await F.Settings(), new SelfHost(new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token));
    await cmd.ExecuteImpl(new RunHostCommand.Settings { AssemblyNames = "Centazio.Sample.AppSheet", Environments = [CentazioConstants.DEFAULT_ENVIRONMENT] });
  }
}
