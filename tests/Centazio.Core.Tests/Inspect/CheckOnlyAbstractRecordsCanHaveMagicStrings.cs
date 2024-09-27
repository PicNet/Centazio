﻿using System.Reflection;
using Centazio.Core.Ctl.Entities;

namespace Centazio.Core.Tests.Inspect;

public class CheckOnlyAbstractRecordsCanHaveMagicStrings {

  private readonly Dictionary<string, List<string>> ALLOWED = new () {
    { "ValidString", ["Value"] },
    { "EntityIntraSysMap", ["LastError"] },
    { "ObjectState", ["LastRunMessage", "LastRunException"] },
    { "StagedEntity", ["IgnoreReason"] },
    { "CoreEntityName", ["Name"] }
  };
  
  [Test] public void Test_string_description_pattern() {
    var types = typeof(StagedEntity).Assembly.GetTypes()
        .Where(t => IsRecord(t) && !t.IsAbstract && t.Namespace != "Centazio.Core.Settings" && t.Namespace != "Centazio.Core.Secrets" && !t.FullName!.EndsWith("+Dto"))
        .ToList();
    var errors = new List<string>();
    types.ForEach(type => {
      var ignore = ALLOWED.TryGetValue(type.Name, out var value) ? value : [];
      var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
          .Where(p => p.PropertyType == typeof(string) && !ignore.Contains(p.Name))
          .ToList();
      if (props.Any()) errors.Add($"Type[{type.FullName}] PROPS[{String.Join(", ", props.Select(p => p.Name))}]");
    });
    Assert.That(errors, Is.Empty, String.Join("\n", errors));
    bool IsRecord(Type t) => t.GetMethods().Any(m => m.Name == "<Clone>$");
  }
}