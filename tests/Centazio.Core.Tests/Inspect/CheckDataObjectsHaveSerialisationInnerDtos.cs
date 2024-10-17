using System.Reflection;
using System.Text.Json.Serialization;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;

namespace Centazio.Core.Tests.Inspect;

public class CheckDataObjectsHaveSerialisationInnerDtos {

  [Test] public void Test_all_data_objects_follow_Dto_pattern() {
    var types = typeof(StagedEntity).Assembly.GetTypes()
        .Where(t => t is { Namespace: "Centazio.Core.Ctl.Entities", IsEnum: false, IsInterface: false } 
            && !(t is { IsAbstract: true, IsSealed: true })) // ignores static classes like StagedEntityListExtensions
        .Where(t => t.BaseType != typeof(Map.CoreToSystemMap)) // ignore these
        .ToList();
    var bases = types.Where(t => t.FullName!.IndexOf('+') < 0).ToList();
    bases.ForEach(t => ValidateDataObject(t, types));
  }

  private static readonly Dictionary<string, List<string>> IGNORE_NON_NULLS = new() { 
    { nameof(ObjectState), [nameof(ObjectState.ObjectIsCoreEntityType), nameof(ObjectState.ObjectIsSystemEntityType)] },
  };
  private static readonly Dictionary<string, List<string>> IGNORE_SETTERS = new() {
    { nameof(Map.CoreToSystemMap), [nameof( Map.CoreToSystemMap.SystemEntityChecksum)] }
  };
  
  private void ValidateDataObject(Type baset, List<Type> types) {
    if (baset.Name.EndsWith("Map") && (baset.Name.StartsWith("CoreAnd") || baset.Name.StartsWith("CoreSystemAnd"))) return;
    var baseprops = baset.GetProperties().Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>(true) is null).ToList();
    var basenames = baseprops.Select(p => p.Name).ToList();
    var dtot = types.Find(t => t.FullName == baset.FullName + "+Dto") ?? throw new Exception($"{baset.FullName}+Dto not found");
    var dtoprops = dtot.GetProperties().Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>(true) is null).ToList();
    var dtonames = dtoprops.Select(p => p.Name).ToList();
    var dtoignore = IGNORE_NON_NULLS.TryGetValue(baset.Name, out var value) ? value : [];
    var nonnulls = dtoprops.Where(p => !ReflectionUtils.IsNullable(p) && !dtoignore.Contains(p.Name)).ToList();
    var setterstoignore = IGNORE_SETTERS.TryGetValue(baset.Name, out var value2) ? value2 : [];
    var setters = baseprops.Where(p => !setterstoignore.Contains(p.Name) && p.SetMethod is not null && p.SetMethod.IsPublic).ToList();
    var extra = dtonames.Where(n => !basenames.Contains(n)).ToList();
    var missing = basenames.Where(n => !dtonames.Contains(n)).ToList();
    
    Assert.That(dtot.GetInterfaces().SingleOrDefault(t => t.Name.Equals("IDto`1")), Is.Not.Null, $"{baset.Name}#Dto does not implement IDto<>");
    Assert.That(baset.GetConstructors().All(c => c.IsPrivate), Is.True, $"{baset.Name} has public constructor");
    Assert.That(setters, Is.Empty, $"{baset.Name} has public setters: {String.Join(",", setters)}");
    Assert.That(nonnulls.Any(), Is.False, $"{baset.Name}#Dto has non-nullable properties: {String.Join(',', nonnulls.Select(p => p.Name))}");
    Assert.That(extra.Any(), Is.False, $"{baset.Name}#Dto has properties not found in base type: {String.Join(',', extra)}");
    Assert.That(missing.Any(), Is.False, $"{baset.Name}#Dto has missing properties found in base type: {String.Join(',', missing)}");
  }

}