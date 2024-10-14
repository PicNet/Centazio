using System.Text.Json;
using Centazio.Core.Ctl.Entities;

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
    var obj = new TestValidStrings { Str1 = nameof(TestValidStrings.Str1), Str2 = nameof(TestValidStrings.Str2) };
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
    var obj = new TestValidStrings { Str1 = nameof(TestValidStrings.Str1), Str2 = nameof(TestValidStrings.Str2) };
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
    var json = Json.Serialize(bt);
    var bt2 = Json.Deserialize<BaseT.Dto>(json).ToBase();
    
    Assert.That(json, Is.EqualTo(@"{""Status"":""Idle"",""Str1"":""Test_dto_pattern""}"));
    Assert.That(bt2, Is.EqualTo(bt));
    
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
}