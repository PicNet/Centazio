using NetArchTest.Rules;

namespace Centazio.Core.Tests.Inspect;

public class CheckNamespaceDependencies {

  private readonly Types centazio = Types.InAssembly(typeof(IntegrationBase<,>).Assembly); 
  private readonly List<(string Namespace1, string Namespace2)> disallowed = [
    (NS(nameof(Read)), NS(nameof(Promote))),
    (NS(nameof(Read)), NS(nameof(Write))),
    (NS(nameof(Promote)), NS(nameof(Write))),
    
    (NS(nameof(Stage)), NS(nameof(Ctl))),
    (NS(nameof(Stage)), NS(nameof(CoreRepo))),
    (NS(nameof(Ctl)), NS(nameof(CoreRepo))),
  ];
  private static string NS(string suffix) => String.Join(nameof(Centazio), nameof(Core), suffix);

  [Test] public void Test_namespace_dependencies() {
    disallowed.ForEach(pair => {
      Assert.That(centazio.That().ResideInNamespace(pair.Namespace1).ShouldNot().HaveDependencyOn(pair.Namespace2).GetResult().IsSuccessful);
      Assert.That(centazio.That().ResideInNamespace(pair.Namespace2).ShouldNot().HaveDependencyOn(pair.Namespace1).GetResult().IsSuccessful);
    });
  }

}