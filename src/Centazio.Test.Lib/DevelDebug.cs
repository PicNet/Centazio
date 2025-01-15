namespace Centazio.Test.Lib;

public class DevelDebug {

  internal static Action<string> TargetWriteLine = Console.WriteLine;
  
  public static void WriteLine(string line) => TargetWriteLine(line);

}