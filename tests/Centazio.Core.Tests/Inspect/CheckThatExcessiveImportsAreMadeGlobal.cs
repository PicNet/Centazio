namespace Centazio.Core.Tests.Inspect;

public class CheckThatExcessiveImportsAreMadeGlobal {

  private readonly List<string> IGNORE = ["generated", "templates"];
  private readonly int LIMIT = 5;
  
  [Test] public void Go() {
    var csprojs = InspectUtils.GetSolnFiles(null, "*.csproj")
        .Where(f => !IGNORE.Any(i => f.IndexOf(i, StringComparison.Ordinal) > 0))
        .ToList();
    var errors = new List<string>();
    csprojs.ForEach(proj => {
      var counts = new Dictionary<string, int>();
      var fname = proj.Split(Path.DirectorySeparatorChar).Last(); 
      var dir = proj.Replace(fname, String.Empty);
      InspectUtils.GetSolnFiles(dir, "*.cs").ForEach(file => {
        var contents = File.ReadAllText(file);
        // using NetArchTest.Rules;
        Regex.Matches(contents, "^using ([^;]+);").ForEach(m => {
          var namepsace = m.Groups[1].Value;
          counts.TryAdd(namepsace, 0);
          counts[namepsace]++;
        });
      });
      counts.Where(kvp => kvp.Value > LIMIT)
          .ForEach(kvp => errors.Add($"project[{fname}] - namespace[{kvp.Key}] is used (with 'using') [{kvp.Value}] times and should be instead be replaced with global usings")); 
      
    });
    Assert.That(errors, Is.Empty, String.Join("\n", errors));
  }
}