﻿using System.Text.RegularExpressions;

namespace Centazio.Core.Tests.Inspect;

public class CheckDependenciesBetweenProjects {

  [Test] public void Check_project_references(){
    var errors = new List<string>();
    var files = InspectUtils.GetSolnFiles(null, "*.csproj");
    var dependencies = ParseDependencies(files);
    if (dependencies["Centazio.Core"].Any()) errors.Add("Centazio.Core should have no project dependencies");
    if (dependencies["Centazio.Test.Lib"].Count > 1 || dependencies["Centazio.Test.Lib"].Single() != "Centazio.Core") errors.Add("Centazio.Test.Lib should at most depend on Centazio.Core");
    
    dependencies.Keys.Where(k => k.IndexOf(".Tests", StringComparison.OrdinalIgnoreCase) >= 0).ForEach(testproj => {
      var target = testproj.Replace(".Tests", "");
      var bad = dependencies[testproj].Where(d => !new [] { "Centazio.Core", "Centazio.Test.Lib", target }.Contains(d)).ToList();
      if (bad.Any()) errors.Add($"Test Project [{testproj}] should at most depend on 'Centazio.Core', 'Centazio.Test.Lib' and '{target}'.  Had extra dependencies: " + String.Join(",", bad));
    });
    dependencies.Keys.Where(k => k.IndexOf(".Tests", StringComparison.OrdinalIgnoreCase) < 0).ForEach(proj => {
      Console.WriteLine("Proj: " + proj + " DEPS: " + String.Join(",", dependencies[proj]));
    });
    Assert.That(errors, Is.Empty, String.Join("\n", errors));
  }

  private Dictionary<string, List<string>> ParseDependencies(List<string> files) {
    return files.ToDictionary(TrimProj, path => Regex.Matches(
        File.ReadAllText(path), 
        "<ProjectReference\\sInclude=\"(.+)\"\\s\\/>")
        .Select(m => TrimProj(m.Groups[1].Value))
        .ToList());
    
    string TrimProj(string file) => file.Split("\\").Last().Replace(".csproj", "");
  }

}