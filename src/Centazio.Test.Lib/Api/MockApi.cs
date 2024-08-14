using System.Runtime.CompilerServices;
using RichardSzalay.MockHttp;

namespace Centazio.Test.Lib.Api;

public class MockApi : IDisposable {
  private bool disposed; 
  private readonly MockHttpMessageHandler mock = new();
  private readonly List<Operation> operations = [];
  
  public MockHttpMessageHandler Mock => mock;

  public static implicit operator HttpClient(MockApi mock) => mock.Mock.ToHttpClient();
  
  public void Initialise([CallerFilePath] string test = "", [CallerMemberName] string method = "") {
    if (disposed) throw new("MockApi should be created independently for each test.  Current MockApi has been disposed."); 
    if (operations.Any()) throw new("MockApi should be created independently for each test.  Initialising a MockApi that has already been initialised.");
    
    var scripts = new [] { test.Replace(".cs", $"_{method}.api"), test.Replace(".cs", $".api") };
    var scriptfn = scripts.FirstOrDefault(File.Exists) ?? throw new($"MockApi could not find any of the possible api script files for the current test.  Checked for the following:\n\t{String.Join("\n\t", scripts)}");
    
    Parse(scriptfn);
    PrepareRequests();
  }

  private void Parse(string file) {
    if (disposed) throw new("MockApi should be created independently for each test.  Current MockApi has been disposed.");
    
    File.ReadAllLines(file).ToList().ForEach(_ => {
      operations.Add(new()); // todo
    });
  }
  
  private void PrepareRequests() {
    if (disposed) throw new("MockApi should be created independently for each test.  Current MockApi has been disposed.");
    if (!operations.Any()) throw new("MockApi could not find any mocked Http operations in the specified script file");
    
    operations.ForEach(op => {
      mock.When(op.Method, op.Url).Respond(op.Response);
    });
    
  }
  
  public void Dispose() { 
    if (disposed) throw new("MockApi should be created independently for each test.  Current MockApi has been disposed.");
    disposed = true;
    operations.Clear();
    mock.Dispose();
  }

}

internal class Operation {
  public HttpMethod Method { get; set; } = null!;
  public string Url { get; set; } = null!;
  public HttpContent Response { get; set; } = null!;
}