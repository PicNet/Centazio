using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Centazio.Cli.Infra.Misc;

public class CommandResults(string command, string args, string dir, string @out, string err, bool newwindow) {
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
  CommandResults DotNet(string args, string? cwd = null, bool quiet = false, bool newwindow = false);
  CommandResults Az(string args, string? cwd = null, bool quiet = false, bool newwindow = false);
  CommandResults Func(string args, string? cwd = null, bool quiet = false, bool newwindow = false);
  CommandResults Lambda(string args, string? cwd = null, bool quiet = false, bool newwindow = false);
  CommandResults Run(string cmd, string args, string? cwd = null, bool quiet = false, bool newwindow = false, bool checktool = false);
}

public class CommandRunner : ICommandRunner {
  private string AzCommand => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "az" :
      File.Exists(@"C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd") 
          ? @"C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd" 
          : @"C:\Program Files (x86)\Microsoft SDKs\Azure\CLI2\wbin\az.cmd";

  private readonly Dictionary<string, bool> installed = new();
  
  public CommandResults DotNet(string args, string? cwd = null, bool quiet = false, bool newwindow = false) => Run("dotnet", args, cwd, quiet, newwindow);
  public CommandResults Az(string args, string? cwd = null, bool quiet = false, bool newwindow = false) => Run(AzCommand, args, cwd, quiet, newwindow);
  public CommandResults Func(string args, string? cwd = null, bool quiet = false, bool newwindow = false) => Run("func", args, cwd, quiet, newwindow);
  public CommandResults Lambda(string args, string? cwd = null, bool quiet = false, bool newwindow = false) => Run("dotnet-lambda", args, cwd, quiet, newwindow);
  public CommandResults Run(string command, string args, string? cwd = null, bool quiet = false, bool newwindow = false, bool checktool = true) {
    if (checktool && !CheckInstalled(command)) return new CommandResults(command, args, cwd ?? String.Empty, String.Empty, String.Empty, newwindow);
    
    var cmdname = new FileInfo(command).Name.Split('.').First();
    if (!quiet) Log.Information($"running[{cmdname}] args[{args}] cwd[{cwd}]");
    cwd ??= Env.IsUnitTest() ? FsUtils.GetDevPath() : Environment.CurrentDirectory;
    
    var output = new StringBuilder();
    var error = new StringBuilder();
    using var p = new Process {
      StartInfo = {
        FileName = command, 
        Arguments = args, 
        RedirectStandardOutput = !newwindow, 
        RedirectStandardError = !newwindow,
        UseShellExecute = newwindow,
        WorkingDirectory = cwd
      }
    };
    p.OutputDataReceived += (_, o) => OnString(o.Data ?? String.Empty, output, quiet, false);
    p.ErrorDataReceived += (_, o) => OnString(o.Data ?? String.Empty, error, quiet, true);
    
    if (newwindow) RunProcessNewWindow(p);
    else RunProcess(p);
    
    var code = p.ExitCode;
    var err = error.ToString().Trim();
    var outp = output.ToString().Trim();
    // -1073741510 is CTRL+C so can be ignored
    if (code != -1073741510 && (code != 0 || err.Length > 0)) 
      throw new Exception($"cmd[{cmdname}] args[{args}] cwd[{cwd}] exitcode[{code}] output:\n{outp}\n\nerror:\n{err}");
    return new CommandResults(cmdname, args, cwd, outp, err, newwindow);
  }

  private bool CheckInstalled(string command) {
    var tool = command.Split('\\').Last();
    if (installed.TryGetValue(tool, out var isinstalled)) { return isinstalled; }
    
    var results = Run(command, "--version", quiet: true, checktool: false, cwd: Environment.CurrentDirectory);
    isinstalled = String.IsNullOrWhiteSpace(results.Err);
    if (!isinstalled) {
      Console.WriteLine($"tool '{tool}' is not installed.");
    }
    return (installed[tool] = isinstalled);
  }

  private void RunProcess(Process p) {
    p.Start();
    p.BeginOutputReadLine();
    p.BeginErrorReadLine();
    p.WaitForExit();
  }

  private void RunProcessNewWindow(Process p) { 
    p.Start();
    p.WaitForExit();
    // does not read output/error streams
  }
  
  private void OnString(string msg, StringBuilder str, bool quiet, bool error) {
    if (String.IsNullOrWhiteSpace(msg)) return;
    
    if (error) Log.Error(msg);
    else if (!quiet) Log.Information(msg);
    str.AppendLine(msg);
  }
}