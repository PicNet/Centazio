﻿using Centazio.Core.Runner;
using Centazio.Core.Stage;

namespace Centazio.Core.Tests.Inspect;

public class CheckOnlyAbstractRecordsCanHaveMagicStrings {

  private readonly Dictionary<string, List<string>> ALLOWED = new () {
    { nameof(ValidString), [nameof(ValidString.Value)] },
    { nameof(Map.CoreToSysMap), [nameof( Map.CoreToSysMap.LastError)] },
    { nameof(ObjectState), [nameof(ObjectState.LastRunMessage), nameof(ObjectState.LastRunException)] },
    { nameof(StagedEntity), [nameof(StagedEntity.IgnoreReason)] },
    { nameof(ValidCron), [nameof(ValidCron.Expression)] },
    { nameof(EntityChange), ["*"] },
    { nameof(FunctionConfig), [nameof(FunctionConfig.FunctionPollExpression)] },
    { nameof(TimerChangeTrigger), [nameof(TimerChangeTrigger.Expression)]},
    { "*", [nameof(Checksum), nameof(ILoggable.LoggableValue)] }
  };
  
  [Test] public void Test_string_description_pattern() {
    var types = typeof(StagedEntity).Assembly.GetTypes()
        .Where(t => ReflectionUtils.IsRecord(t) && !t.IsAbstract && t.Namespace != "Centazio.Core.Settings" && t.Namespace != "Centazio.Core.Secrets" && t.FullName is not null && !t.FullName.EndsWith("+Dto"))
        .ToList();
    var errors = new List<string>();
    types.ForEach(type => {
      var ignore = Allowed(type);
      if (ignore.FirstOrDefault() == "*") return;
      var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
          .Where(p => p.PropertyType == typeof(string) && !ignore.Contains(p.Name))
          .ToList();
      if (props.Any()) errors.Add($"Type[{type.FullName}] PROPS[{String.Join(", ", props.Select(p => p.Name))}]");
    });
    Assert.That(errors, Is.Empty, String.Join("\n", errors));
    
    List<string> Allowed(Type t) {
      var match = ALLOWED.Keys.FirstOrDefault(k => t.Name == k || t.FullName!.Split('.').Last().StartsWith(k + '+'));
      if (match is null) return ALLOWED["*"];
      return ALLOWED[match].Concat(ALLOWED["*"]).ToList();
    }
  }
}