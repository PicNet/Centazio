namespace Centazio.Core.Misc;

public class DataFlowLogger {

  public static readonly string PREFIX = "Data Flow:";
  
  public static void Log(string from, string obj, string to, List<string> flows) {
    if (!flows.Any()) return;
    var flowsstr = flows.Count == 1 ? flows[0] : String.Join("", flows.Select(f => $"\n\t{f}"));
    Serilog.Log.Information($"{PREFIX} {from}.{obj} -> {to}: {flowsstr}");
  }

}