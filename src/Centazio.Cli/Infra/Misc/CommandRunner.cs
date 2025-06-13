using System.Diagnostics;
using System.Text;

namespace Centazio.Cli.Infra.Misc;

public record CommandResults(Process? Process, string Command, string Args, string Dir, string Out, string Err) {
  public bool Success => String.IsNullOrWhiteSpace(Err);
  public override string ToString() => $"{Command} {Args}";

}

public interface ICommandRunner {
  Task<CommandResults> DotNet(string args, string? cwd = null, bool quiet = false, string? input = null);
  Task<CommandResults> Az(string args, string? cwd = null, bool quiet = false, string? input = null);
  Task<CommandResults> Func(string args, string? cwd = null, bool quiet = false, string? input = null);
  Task<CommandResults> Lambda(string args, string? cwd = null, bool quiet = false, string? input = null);
  Task<CommandResults> Aws(string args, string? cwd = null, bool quiet = false, string? input = null);
  Task<CommandResults> Docker(string args, string? cwd = null, bool quiet = false, string? input = null);
  Task<CommandResults> Run(string cmd, string args, string? cwd = null, bool quiet = false, bool checktool = false, string? input = null);
}

public class CommandRunner : ICommandRunner {
  private string AzCommand => Env.IsLinux ? "az" :
      File.Exists(@"C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd") 
          ? @"C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd" 
          : @"C:\Program Files (x86)\Microsoft SDKs\Azure\CLI2\wbin\az.cmd";

  private readonly Dictionary<string, bool> installed = [];
  
  public Task<CommandResults> DotNet(string args, string? cwd = null, bool quiet = false, string? input = null) => Run("dotnet", args, cwd, quiet, input: input);
  public Task<CommandResults> Az(string args, string? cwd = null, bool quiet = false, string? input = null) => Run(AzCommand, args, cwd, quiet, input: input);
  public Task<CommandResults> Func(string args, string? cwd = null, bool quiet = false, string? input = null) => Run("func", args, cwd, quiet, input: input);
  public Task<CommandResults> Lambda(string args, string? cwd = null, bool quiet = false, string? input = null) => Run("dotnet-lambda", args, cwd, quiet, input: input);
  public Task<CommandResults> Aws(string args, string? cwd = null, bool quiet = false, string? input = null) => Run("aws", args, cwd, quiet, input: input);
  public Task<CommandResults> Docker(string args, string? cwd = null, bool quiet = false, string? input = null) => Run("docker", args, cwd, quiet, input: input);

  public async Task<CommandResults> Run(string command, string args, string? cwd = null, bool quiet = false, bool checktool = true, string? input = null) {
    // if (newwindow && input is not null) throw new ArgumentException($"CommandRunner does not support `input` when `newwindow=true`"); // at least on linux
    
    cwd ??= Env.IsUnitTest ? FsUtils.GetCentazioPath() : Environment.CurrentDirectory;
    var results = new CommandResults(null, command, args, cwd, String.Empty, String.Empty);
    
    if (!quiet) Log.Information($"running[{command}] args[{args}] cwd[{cwd}]");
    if (checktool && !await CheckInstalled(command)) return results;
    
    var output = new StringBuilder();
    var error = new StringBuilder();
    using var p = new Process {
      StartInfo = {
        FileName = command, 
        Arguments = args, 
        RedirectStandardInput = input is not null,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        WorkingDirectory = cwd
      }
    };
    p.OutputDataReceived += (_, o) => OnString(o.Data ?? String.Empty, output, quiet, false);
    p.ErrorDataReceived += (_, o) => OnString(o.Data ?? String.Empty, error, quiet, true);
    
    await RunProcess(p, input);

    var code = p.ExitCode;
    var err = error.ToString().Trim();
    var outp = output.ToString().Trim();
    // -1073741510 is CTRL+C so can be ignored
    if (code != -1073741510 && (code != 0 || err.Length > 0)) throw new Exception($"cmd[{command}] args[{args}] cwd[{cwd}] exitcode[{code}] output:\n{outp}\n\nerror:\n{err}");
    return results with { Process = p, Out = outp, Err = err };
  }

  private async Task<bool> CheckInstalled(string command) {
    var tool = command.Split('\\').Last();
    if (installed.TryGetValue(tool, out var isinstalled)) { return isinstalled; }
    
    var results = await Run(command, "--version", quiet: true, checktool: false, cwd: Environment.CurrentDirectory);
    isinstalled = String.IsNullOrWhiteSpace(results.Err);
    if (!isinstalled) {
      Console.WriteLine($"tool '{tool}' is not installed.");
    }
    return installed[tool] = isinstalled;
  }

  private Task RunProcess(Process p, string? input) {
    return Task.Run(() => {
      p.Start();
      InjectInput();
      p.BeginOutputReadLine();
      p.BeginErrorReadLine();
      p.WaitForExit();
    });
    
    void InjectInput() {
      if (input is null) return;
      p.StandardInput.WriteLine(input);
      p.StandardInput.Close();
    }
  }

  private void OnString(string msg, StringBuilder str, bool quiet, bool error) {
    if (String.IsNullOrWhiteSpace(msg)) return;
    
    if (error) Log.Error(msg);
    else if (!quiet) Log.Information(msg);
    str.AppendLine(msg);
  }
}