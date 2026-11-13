using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Centazio.Cli.Infra.Misc;

// tools write warnings and progress to stderr (eg: `az` update notices), so only the exit code signals failure
public record CommandResults(Process? Process, string Command, string Args, string Dir, string Out, string Err, int Code) {
  public bool Success => Code == 0;
  public override string ToString() => $"{Command} {CommandRunner.Redact(Args)}";

}

public interface ICommandRunner {
  Task<CommandResults> DotNet(string args, string? cwd = null, bool quiet = false, string? input = null);
  Task<CommandResults> Az(string args, string? cwd = null, bool quiet = false, string? input = null);
  Task<CommandResults> Func(string args, string? cwd = null, bool quiet = false, string? input = null);
  Task<CommandResults> Lambda(string args, string? cwd = null, bool quiet = false, string? input = null);
  Task<CommandResults> Aws(string args, string? cwd = null, bool quiet = false, string? input = null);
  Task<CommandResults> Run(string cmd, string args, string? cwd = null, bool quiet = false, bool checktool = false, string? input = null);
}

public class CommandRunner : ICommandRunner {
  private string AzCommand => Env.IsLinux ? "az" :
      File.Exists(@"C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd") 
          ? @"C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd" 
          : @"C:\Program Files (x86)\Microsoft SDKs\Azure\CLI2\wbin\az.cmd";

  private static readonly TimeSpan STREAM_GRACE = TimeSpan.FromSeconds(2);
  
  private readonly Dictionary<string, bool> installed = [];

  // keeps credentials passed as arguments (eg: `nuget push --api-key <key>`) out of logs and exception messages
  internal static string Redact(string args) => Regex.Replace(args, @"(--api-key|--password|--secret)(\s+|=)(""[^""]*""|\S+)", "$1$2***", RegexOptions.IgnoreCase);
  
  public Task<CommandResults> DotNet(string args, string? cwd = null, bool quiet = false, string? input = null) => Run("dotnet", args, cwd, quiet, input: input);
  public Task<CommandResults> Az(string args, string? cwd = null, bool quiet = false, string? input = null) => Run(AzCommand, args, cwd, quiet, input: input);
  public Task<CommandResults> Func(string args, string? cwd = null, bool quiet = false, string? input = null) => Run("func", args, cwd, quiet, input: input);
  public Task<CommandResults> Lambda(string args, string? cwd = null, bool quiet = false, string? input = null) => Run("dotnet-lambda", args, cwd, quiet, input: input);
  public Task<CommandResults> Aws(string args, string? cwd = null, bool quiet = false, string? input = null) => Run("aws", args, cwd, quiet, input: input);

  public async Task<CommandResults> Run(string command, string args, string? cwd = null, bool quiet = false, bool checktool = true, string? input = null) {
    // if (newwindow && input is not null) throw new ArgumentException($"CommandRunner does not support `input` when `newwindow=true`"); // at least on linux
    
    cwd ??= Env.IsUnitTest ? FsUtils.GetCentazioPath() : Environment.CurrentDirectory;
    var results = new CommandResults(null, command, args, cwd, String.Empty, String.Empty, -1);
    
    if (!quiet) Log.Information($"running[{command}] args[{Redact(args)}] cwd[{cwd}]");
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
    var (outeof, erreof) = (new TaskCompletionSource(), new TaskCompletionSource());
    p.OutputDataReceived += (_, o) => OnData(o.Data, output, outeof, quiet, false);
    p.ErrorDataReceived += (_, o) => OnData(o.Data, error, erreof, quiet, true);
    
    await RunProcess(p, input);
    await Task.WhenAny(Task.WhenAll(outeof.Task, erreof.Task), Task.Delay(STREAM_GRACE));

    var code = p.ExitCode;
    var err = error.ToString().Trim();
    var outp = output.ToString().Trim();
    // -1073741510 is CTRL+C so can be ignored
    if (code != -1073741510 && code != 0) throw new Exception($"cmd[{command}] args[{Redact(args)}] cwd[{cwd}] exitcode[{code}] output:\n{outp}\n\nerror:\n{err}");
    return results with { Process = p, Out = outp, Err = err, Code = code };
  }

  private async Task<bool> CheckInstalled(string command) {
    var tool = command.Split('\\').Last();
    if (installed.TryGetValue(tool, out var isinstalled)) { return isinstalled; }
    
    var results = await Run(command, "--version", quiet: true, checktool: false, cwd: Environment.CurrentDirectory);
    isinstalled = results.Success;
    if (!isinstalled) {
      Console.WriteLine($"tool '{tool}' is not installed.");
    }
    return installed[tool] = isinstalled;
  }

  // waits for the process only: `WaitForExit()` also waits for the redirected streams to close, and processes the
  //    command leaves running (eg: the MSBuild node and Roslyn server `dotnet build` reuses) hold them open
  private Task RunProcess(Process p, string? input) {
    return Task.Run(() => {
      p.Start();
      InjectInput();
      p.BeginOutputReadLine();
      p.BeginErrorReadLine();
      p.WaitForExit(Int32.MaxValue);
    });
    
    void InjectInput() {
      if (input is null) return;
      p.StandardInput.WriteLine(input);
      p.StandardInput.Close();
    }
  }

  private void OnData(string? msg, StringBuilder str, TaskCompletionSource eof, bool quiet, bool error) {
    if (msg is null) eof.TrySetResult();
    if (String.IsNullOrWhiteSpace(msg)) return;
    
    if (error) Log.Error(msg);
    else if (!quiet) Log.Information(msg);
    str.AppendLine(msg);
  }
}