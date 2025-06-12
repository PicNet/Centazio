using Centazio.Cli.Commands.Host;
using Centazio.Core;
using Centazio.Hosts.Self;

namespace Centazio.Cli.Tests.Commands;

public class RunHostCommandTests {

  [Test] public async Task Test_that_running_self_host_works_as_expected() {
    // var command = "./centazio az func simulate Centazio.Sample.AppSheet";
    var cmd = new RunHostCommand(await F.Settings(), new SelfHost());
    // todo: improve/implement this test or move to SelfHostTests
    await cmd.ExecuteImpl(new RunHostCommand.Settings {
      AssemblyNames = "Centazio.TestFunctions",
      Environments = [CentazioConstants.DEFAULT_ENVIRONMENT],
      // todo GT: how the hell can we set this?
      // EnvironmentsList = {  "" }
    });
  }

}