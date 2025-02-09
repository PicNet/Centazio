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
      Assert.That(InspectUtils.CentazioTypes.That().ResideInNamespace(pair.Namespace1).ShouldNot().HaveDependencyOn(pair.Namespace2).GetResult().IsSuccessful);
      Assert.That(InspectUtils.CentazioTypes.That().ResideInNamespace(pair.Namespace2).ShouldNot().HaveDependencyOn(pair.Namespace1).GetResult().IsSuccessful);
    });
    ONE_WAY_DISALLOWED.ForEach(pair => {
      Assert.That(InspectUtils.CentazioTypes.That().ResideInNamespace(pair.Namespace1).ShouldNot().HaveDependencyOn(pair.Namespace2).GetResult().IsSuccessful);
    });
  }

}