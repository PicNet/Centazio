using System.Reflection;
using System.Runtime.CompilerServices;

namespace Centazio.Core.Tests.Inspect;

public class CheckThatInternalsVisibleToIsOnlyAvailableToTestProjects {

  [Test] public void Go() {
    var errors = new List<string>();
    InspectUtils.GetCentazioDllFiles().ForEach(dll => {
      var filename = dll.Split("\\").Last();
      var istest = filename.IndexOf("test", StringComparison.OrdinalIgnoreCase) >= 0;
      
      var friends = Assembly.LoadFile(dll).CustomAttributes
          .Where(att => att.AttributeType == typeof(InternalsVisibleToAttribute))
          .SelectMany(att => att.ConstructorArguments.Select(a => a.Value?.ToString()))
          .Where(arg => !String.IsNullOrWhiteSpace(arg))
          .Cast<string>()
          .ToList();
      if (istest) {
        if (friends.Any()) errors.Add($"assembly[{filename}] is a test assembly and should not have any friend assemblies: " + String.Join(", ", friends));
        return;
      } 
      var nontests = friends.Where(f => f.IndexOf("Test", StringComparison.Ordinal) < 0).ToList();
      if (nontests.Any())
        errors.Add($"assembly[{filename}] should only have test assemblies as friends: " + String.Join(", ", nontests));
      
    });
    
    Assert.That(errors, Is.Empty, String.Join("\n", errors));
  }

}