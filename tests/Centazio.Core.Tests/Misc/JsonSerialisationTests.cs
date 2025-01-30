﻿using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Core.Types;

namespace Centazio.Core.Tests.Misc;

public class JsonSerialisationTests {

  [Test] public void Test_serialisation_of_valid_strings() {
    var str = new ValidString(nameof(JsonSerialisationTests));
    var serialised = JsonSerializer.Serialize(str);
    var deserialised = JsonSerializer.Deserialize<ValidString>(serialised);
    Assert.That(str, Is.EqualTo(deserialised));
  }
  
  [Test] public void Test_serialisation_of_valid_string_subclasses() {
    var str = new CoreEntityId(nameof(JsonSerialisationTests));
    var serialised = JsonSerializer.Serialize(str);
    var deserialised = JsonSerializer.Deserialize<CoreEntityId>(serialised);
    Assert.That(str, Is.EqualTo(deserialised));
  }

  [Test] public void Test_serialisation_of_objects_with_child_valid_strings() {
    var obj = new TestValidStrings { Str1 = new(nameof(TestValidStrings.Str1)), Str2 = new(nameof(TestValidStrings.Str2)) };
    var serialised = JsonSerializer.Serialize(obj);
    var deserialised = JsonSerializer.Deserialize<TestValidStrings>(serialised);
    Assert.That(obj, Is.EqualTo(deserialised));
  }
  
  [Test] public void Test_serialisation_of_objects_with_child_valid_string_subclasses() {
    var obj = new TestValidStringSubclasses { CoreId = new(nameof(TestValidStrings.Str1)), SystemId = new(nameof(TestValidStrings.Str2)) };
    var serialised = JsonSerializer.Serialize(obj);
    var deserialised = JsonSerializer.Deserialize<TestValidStringSubclasses>(serialised);
    Assert.That(obj, Is.EqualTo(deserialised));
  }
  
  [Test] public void Test_serialisation_of_object_dtos_with_child_valid_strings() {
    var obj = new TestValidStrings { Str1 = new(nameof(TestValidStrings.Str1)), Str2 = new(nameof(TestValidStrings.Str2)) };
    var serialised = Json.Serialize(obj);
    var deserialised = JsonSerializer.Deserialize<TestValidStrings.Dto>(serialised)!.FromDto();
    Assert.That(obj, Is.EqualTo(deserialised));
  }
  
  [Test] public void Test_serialisation_of_object_dtos_with_child_valid_string_subclasses() {
    var obj = new TestValidStringSubclasses { CoreId = new(nameof(TestValidStrings.Str1)), SystemId = new(nameof(TestValidStrings.Str2)) };
    var serialised = Json.Serialize(obj);
    var deserialised = JsonSerializer.Deserialize<TestValidStringSubclasses.Dto>(serialised)!.FromDto();
    Assert.That(obj, Is.EqualTo(deserialised));
  }
  
  [Test] public void Test_dto_pattern() {
    var bt = new BaseT { Status = ESystemStateStatus.Idle, Str1 = new(nameof(Test_dto_pattern)) };
    TestDtoImpl(bt);
  }
  
  [Test] public void Test_real_dtos() {
    var str = nameof(Test_real_dtos);
    
    // Centazio.Core.Ctl.Entities.ObjectState+Dto
    var oscet = new ObjectState(new(str), new(str), new CoreEntityTypeName(str), UtcDate.UtcNow, true);
    var oscet2 = Json.Deserialize<ObjectState>(Json.Serialize(oscet));
    Assert.That(oscet2, Is.EqualTo(oscet));
    
    var osset = new ObjectState(new(str), new(str), new SystemEntityTypeName(str), UtcDate.UtcNow, true);
    var osset2 = Json.Deserialize<ObjectState>(Json.Serialize(osset));
    Assert.That(osset2, Is.EqualTo(osset));
    
    // Centazio.Core.Ctl.Entities.SystemState+Dto
    var ss = SystemState.Create(new(str), new(str));
    var ss2 = Json.Deserialize<SystemState>(Json.Serialize(ss));
    Assert.That(ss2, Is.EqualTo(ss));
    
    // Centazio.Core.Ctl.Entities.StagedEntity+Dto
    var se = new StagedEntity(Guid.NewGuid(), new(str), new(str), UtcDate.UtcNow, new("data"), new("checksum"));
    var se2 = Json.Deserialize<StagedEntity>(Json.Serialize(se));
    Assert.That(se2, Is.EqualTo(se));
    
    // Centazio.Core.Ctl.Entities.Map+CoreToSystem+Dto
    var c2s = new Map.CoreToSysMap(new(str), new(str), new(str), new(str), EEntityMappingStatus.Orphaned, new(str));
    var c2s2 = Json.Deserialize<Map.CoreToSysMap>(Json.Serialize(c2s));
    Assert.That(c2s2, Is.EqualTo(c2s));
  }
  
  [Test] public async Task Test_serialisation_to_HttpContent_respects_interface_JsonIgnore() {
    var body = Json.SerializeToHttpContent(new SystemEntityType("Only this field should be serialised"));
    var json = await body.ReadAsStringAsync();
    Assert.That(json, Does.Contain("\"Prop\":"));
    Assert.That(json, Does.Contain("\"New Name\":"));
    Assert.That(json, Does.Not.Contain("\"JsonPropNameTest\":"));
    Assert.That(json, Does.Not.Contain("\"DisplayName\":"));
    Assert.That(json, Does.Not.Contain("\"SystemId\":"));
    Assert.That(json, Does.Not.Contain("\"LastUpdatedDate\":"));
  }
  
  [Test, Ignore("RespectNullableAnnotations does not work with empty object '{}'")] public void Test_RespectNullableAnnotations() {
    Assert.Throws<JsonException>(() => Json.Deserialize<ObjWithNullables>(@"{""NullStr"": ""NullStr"", ""NonNullStr"": null}"));
    var result = Json.Deserialize<ObjWithNullables>("{}");
    Validator.ValidateObject(result, new ValidationContext(result), true);
    // Validator also, does not check field nullability
  }
  
  private void TestDtoImpl<T>(T baseobj) {
    ArgumentNullException.ThrowIfNull(baseobj);
    
    var json = Json.Serialize(baseobj);
    var deserialised = Json.Deserialize<T>(json); // should automatically detect the Dto 
    
    Assert.That(deserialised, Is.EqualTo(baseobj));
  }
  
  public record ObjWithNullables {
    public string NonNullStr { get; init; } = null!;
    public string? NullStr { get; init; }
    
    public DateTime NonNullDt { get; init; } = DateTime.MinValue;
    public DateTime? NullDt { get; init; }
    
    public TestValidStrings NonNullObj { get; init; } = null!;
    public TestValidStrings? NullObj { get; init; }
  }
  
  public record TestValidStrings {
    public ValidString Str1 { get; init; } = null!;
    public ValidString Str2 { get; init; } = null!;
    
    public record Dto {
      public string? Str1 { get; init; }
      public string? Str2 { get; init; }
      
      public TestValidStrings FromDto() {
        return new TestValidStrings { 
          Str1 = new(Str1 ?? throw new Exception()), 
          Str2 = new(Str2 ?? throw new Exception()) };
      }
    }
  }
  
  public record TestValidStringSubclasses {
    public CoreEntityId CoreId { get; init; } = null!;
    public SystemEntityId SystemId { get; init; } = null!;
    
    public record Dto {
      public string? CoreId { get; init; }
      public string? SystemId { get; init; }
      
      public TestValidStringSubclasses FromDto() {
        return new TestValidStringSubclasses { 
          CoreId = new(CoreId ?? throw new Exception()), 
          SystemId = new(SystemId ?? throw new Exception()) };
      }
    }
  }
  
  public record BaseT {
    public ESystemStateStatus Status { get; set; }
    public ValidString Str1 { get; set; } = null!;
    
    public record Dto {
      public string? Status { get; set; }
      public string? Str1 { get; set; }
      
      public BaseT ToBase() {
        return new BaseT { 
          Status = Enum.Parse<ESystemStateStatus>(Status ?? throw new ArgumentNullException()), 
          Str1 = new(Str1 ?? throw new ArgumentNullException()) 
        };
      }
    }
  }
  
  public record SystemEntityType(string Prop) : ISystemEntity {

    [JsonPropertyName("New Name")] public string JsonPropNameTest => nameof(JsonPropNameTest);
    
    public string DisplayName => Prop;
    public object GetChecksumSubset() => throw new Exception();
    
    // these two fields should be ignored during serialisation
    public SystemEntityId SystemId { get; } = new(Guid.NewGuid().ToString());
    public DateTime LastUpdatedDate { get; } = UtcDate.UtcNow;

  }
}