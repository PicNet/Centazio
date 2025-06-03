namespace Centazio.Core.Tests.Inspect;

public class CheckNamespaceDependencies {

  private readonly List<(string Namespace1, string Namespace2)> BIDI_DISALLOWED = [
    (NS(nameof(Read)), NS(nameof(Promote))),
    (NS(nameof(Read)), NS(nameof(Write))),
    (NS(nameof(Promote)), NS(nameof(Write))),
    
    (NS(nameof(Stage)), NS(nameof(Ctl))),
    (NS(nameof(Stage)), NS(nameof(CoreRepo))),
    (NS(nameof(Stage)), NS(nameof(Write))),
    (NS(nameof(Ctl)), NS(nameof(CoreRepo))),
  ];
  
  private readonly List<(string Namespace1, string Namespace2)> ONE_WAY_DISALLOWED = [
    (NS(nameof(Stage)), NS(nameof(Read))),
    (NS(nameof(Stage)), NS(nameof(Ctl))),
    (NS(nameof(Stage)), NS(nameof(CoreRepo)))
  ];
  private static string NS(string suffix) => String.Join('.', nameof(Centazio), nameof(Core), suffix);

  [Test] public void Test_namespace_dependencies() {
    BIDI_DISALLOWED.ForEach(pair => {
      TestDisallowedDependency(pair.Namespace1, pair.Namespace2);
      TestDisallowedDependency(pair.Namespace2, pair.Namespace1);
    });
    ONE_WAY_DISALLOWED.ForEach(pair => {
      TestDisallowedDependency(pair.Namespace1, pair.Namespace2);
    });
    
    void TestDisallowedDependency(string namespace1, string notallowed) {
      var result = InspectUtils.CentazioTypes.That().ResideInNamespace(namespace1).ShouldNot().HaveDependencyOn(notallowed).GetResult();
      if (result.IsSuccessful) return;
      var message = $"[{namespace1}] should not depend on [{notallowed}]" + 
          $"\nFailing Types:\n\t" + String.Join($"\n\t", result.FailingTypeNames);
      Assert.That(result.IsSuccessful, message);
    }
  }

}