using Centazio.Hosts.Aws;

namespace {{it.FunctionNamespace}}.Aws;

public class Program {
  public static async Task Main() => 
      await new Host().Init(new {{it.ClassName}}Handler());
}