using Centazio.Core.Misc;

namespace Centazio.Core.Tests.Inspect;

public class CheckCorrectUseageOfRecordHierarchies {
  
  private readonly List<string> IGNORE = ["Centazio.Sample"];
  
  [Test] public void Test_record_hierarchies() {
    var errors = new List<string>();
    var records = InspectUtils.LoadCentazioAssemblies()
        .Where(ass => !IGNORE.Contains(ass.GetName().Name ?? throw new Exception()))
        .SelectMany(ass => ass.GetExportedTypes().Where(ReflectionUtils.IsRecord))
        .ToList();
    var resultbases = records.Where(r => r.IsAbstract && r.Name != "OperationResult" && r.Name.Contains("Result", StringComparison.Ordinal)).ToList();
    var allimpls = records.Where(r => !r.IsAbstract).ToList();
    resultbases.ForEach(baserec => {
      var impls = allimpls.Where(baserec.IsAssignableFrom).ToList();
      if (impls.Any()) errors.Add($"abstract 'Result' records should not export any subclasses, i.e. they should be 'internal sealed' and be created using factory methods on the base abstract record. Found the following violations:\n\t" + String.Join("\n\t", impls));
    });
    Assert.That(errors, Is.Empty, "\n\n" + String.Join("\n", errors) + "\n\n\n\n----------------------------------------------\n");
    
  }
}