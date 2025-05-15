using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Centazio.Cli.Infra.Misc;

public class CommandResults(Process? process, string command, string args, string dir, string @out, string err, bool newwindow) {
  
  public Process? Process { get; } = process;
  public string Command { get; } = command.Trim();
  public string Args { get; } = args.Trim();
  public string Dir { get; } = dir.Trim();
  public string Out { get; } = @out.Trim();
  public string Err { get; } = err.Trim();
  public bool NewWindow { get; } = newwindow;

  public bool Success => String.IsNullOrWhiteSpace(Err);
  
  public override string ToString() => $"{Command} {Args}";

}

public interface ICommandRunner {
  CommandResults DotNet(string args, string? cwd = null, bool quiet = false, bool newwindow = false, string? input = null);
  CommandResults Az(string args, string? cwd = null, bool quiet = false, bool newwindow = false, string? input = null);
  CommandResults Func(string args, string? cwd = null, bool quiet = false, bool newwindow = false, string? input = null);
  CommandResults Lambda(string args, string? cwd = null, bool quiet = false, bool newwindow = false, string? input = null);
  CommandResults Aws(string args, string? cwd = null, bool quiet = false, bool newwindow = false, string? input = null);
  CommandResults Docker(string args, string? cwd = null, bool quiet = false, bool newwindow = false, string? input = null);
  CommandResults Run(string cmd, string args, string? cwd = null, bool quiet = false, bool newwindow = false, bool checktool = false, string? input = null);
}

public class CommandRunner : ICommandRunner {
  private string AzCommand => Env.IsLinux ? "az" :
      File.Exists(@"C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd") 
          ? @"C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd" 
          : @"C:\Program Files (x86)\Microsoft SDKs\Azure\CLI2\wbin\az.cmd";

  private readonly Dictionary<string, bool> installed = new();
  
  public CommandResults DotNet(string args, string? cwd = null, bool quiet = false, bool newwindow = false, string? input = null) => Run("dotnet", args, cwd, quiet, newwindow, input: input);
  public CommandResults Az(string args, string? cwd = null, bool quiet = false, bool newwindow = false, string? input = null) => Run(AzCommand, args, cwd, quiet, newwindow, input: input);
  public CommandResults Func(string args, string? cwd = null, bool quiet = false, bool newwindow = false, string? input = null) => Run("func", args, cwd, quiet, newwindow, input: input);
  public CommandResults Lambda(string args, string? cwd = null, bool quiet = false, bool newwindow = false, string? input = null) => Run("dotnet-lambda", args, cwd, quiet, newwindow, input: input);
  public CommandResults Aws(string args, string? cwd = null, bool quiet = false, bool newwindow = false, string? input = null) => Run("aws", args, cwd, quiet, newwindow, input: input);
  public CommandResults Docker(string args, string? cwd = null, bool quiet = false, bool newwindow = false, string? input = null) => Run("docker", args, cwd, quiet, newwindow, input: input);

  public CommandResults Run(string command, string args, string? cwd = null, bool quiet = false, bool newwindow = false, bool checktool = true, string? input = null) {
    cwd ??= Env.IsUnitTest ? FsUtils.GetCentazioPath() : Environment.CurrentDirectory;
    (command, args) = GetPlatformSpecificCommmandAndArgs();
    
    if (!quiet) Log.Information($"running[{command}] args[{args}] cwd[{cwd}]");
    if (checktool && !CheckInstalled(command)) return new CommandResults(null, command, args, cwd, String.Empty, String.Empty, newwindow);
    
    var output = new StringBuilder();
    var error = new StringBuilder();
    using var p = new Process {
      StartInfo = {
        FileName = command, 
        Arguments = args, 
        RedirectStandardInput = input is not null,
        RedirectStandardOutput = !newwindow, 
        RedirectStandardError = !newwindow,
        UseShellExecute = newwindow,
        WorkingDirectory = cwd
      }
    };
    p.OutputDataReceived += (_, o) => OnString(o.Data ?? String.Empty, output, quiet, false);
    p.ErrorDataReceived += (_, o) => OnString(o.Data ?? String.Empty, error, quiet, true);
    
    if (newwindow) {
      RunProcessNewWindow(p, input);
      return new CommandResults(p, command, args, cwd, String.Empty, String.Empty, newwindow);
    }

    RunProcess(p, input);

    var code = p.ExitCode;
    var err = error.ToString().Trim();
    var outp = output.ToString().Trim();
    // -1073741510 is CTRL+C so can be ignored
    if (code != -1073741510 && (code != 0 || err.Length > 0)) 
      throw new Exception($"cmd[{command}] args[{args}] cwd[{cwd}] exitcode[{code}] output:\n{outp}\n\nerror:\n{err}");
    return new CommandResults(p, command, args, cwd, outp, err, newwindow);
    
    (string cmd, string arg) GetPlatformSpecificCommmandAndArgs() {
      return Env.IsLinux && newwindow ? ("x-terminal-emulator", $"-e \"bash -c '{command} {args}'\"") : (command, args);
    }
  }

  private bool CheckInstalled(string command) {
    var tool = command.Split('\\').Last();
    if (installed.TryGetValue(tool, out var isinstalled)) { return isinstalled; }
    
    var results = Run(command, "--version", quiet: true, checktool: false, cwd: Environment.CurrentDirectory);
    isinstalled = String.IsNullOrWhiteSpace(results.Err);
    if (!isinstalled) {
      Console.WriteLine($"tool '{tool}' is not installed.");
    }
    return installed[tool] = isinstalled;
  }

  private void RunProcess(Process p, string? input) {
    p.Start();
    InjectInput(p, input);
    p.BeginOutputReadLine();
    p.BeginErrorReadLine();
    p.WaitForExit();
  }

  private void RunProcessNewWindow(Process p, string? input) {
    p.Start();
    InjectInput(p, input);
    if (!Env.IsLinux) p.WaitForExit();
  }

  private void InjectInput(Process p, string? input) {
    if (input is null) return;

    p.StandardInput.WriteLine(input);
    p.StandardInput.Close();
  }

  private void OnString(string msg, StringBuilder str, bool quiet, bool error) {
    if (String.IsNullOrWhiteSpace(msg)) return;
    
    if (error) Log.Error(msg);
    else if (!quiet) Log.Information(msg);
    str.AppendLine(msg);
  }
}