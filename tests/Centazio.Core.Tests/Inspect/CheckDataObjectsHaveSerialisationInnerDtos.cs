using System.Reflection;
using Centazio.Core.Ctl.Entities;

namespace Centazio.Core.Tests.Inspect;

public class CheckDataObjectsHaveSerialisationInnerDtos {

  [Test] public void Test_all_data_objects_follow_Dto_pattern() {
    var types = typeof(StagedEntity).Assembly.GetTypes()
        .Where(t => t is { Namespace: "Centazio.Core.Ctl.Entities", IsEnum: false })
        .Where(t => t.BaseType != typeof(CoreToExternalMap)) // ignore these
        .ToList();
    var bases = types.Where(t => t.FullName!.IndexOf('+') < 0).ToList();
    bases.ForEach(t => ValidateDataObject(t, types));
  }

  private void ValidateDataObject(Type baset, List<Type> types) {
    var dto = types.Find(t => t.FullName == baset.FullName + "+Dto") ?? throw new Exception($"{baset.FullName}+Dto not found");
    var nonnulls = dto.GetProperties().Where(p => !IsNullable(p)).ToList();
    Assert.That(baset.GetConstructors().All(c => c.IsPrivate), Is.True, $"{baset.Name} has public constructor");
    Assert.That(baset.GetProperties().All(p => p.SetMethod is null || !p.SetMethod.IsPublic), Is.True, $"{baset.Name} has public setters");
    Assert.That(nonnulls.Any(), Is.False, $"{baset.Name}#Dto has non-nullable properties: {String.Join(',', nonnulls.Select(p => p.Name))}");
    Test.Lib.Helpers.DebugWrite($"Type[{baset.Name}] Constructors[{baset.GetConstructors().Length}] Setters[{baset.GetProperties().Count(p => p.SetMethod is not null)}]");

    bool IsNullable(PropertyInfo property) {
      var nullabilityInfoContext = new NullabilityInfoContext();
      var info = nullabilityInfoContext.Create(property);
      return info.WriteState == NullabilityState.Nullable || info.ReadState == NullabilityState.Nullable;
    }
  }

}