using Centazio.Cli.Commands.Host;
using Centazio.Core;
using Centazio.Core.Misc;
using Centazio.Hosts.Self;

namespace Centazio.Cli.Tests.Commands;

public class RunHostCommandTests {

  [Test] public async Task Test_command_runs_for_2_seconds_without_errors() {
    // todo: this appears to be getting stuck in GH Actions, i.e. the 2 second timer is not working
    if (Env.IsGitHubActions) return;
    
    var cmd = new RunHostCommand(await F.Settings(), new SelfHost(new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token));
    await cmd.ExecuteImpl(new RunHostCommand.Settings { AssemblyNames = "Centazio.TestFunctions", Environments = [CentazioConstants.DEFAULT_ENVIRONMENT] });
  }
}
