namespace Centazio.Core.Misc;

public class DataFlowLogger {

  public static readonly string PREFIX = "Data Flow:";
  
  public static void Log(string from, string obj, string to, string message) => 
      Serilog.Log.Information($"{PREFIX} {from}.{obj} -> {to}: {message}");

}