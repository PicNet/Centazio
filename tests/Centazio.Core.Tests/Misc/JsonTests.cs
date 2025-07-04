using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Centazio.Core.Stage;

namespace Centazio.Core.Tests.Misc;

public class JsonTests {

  [Test] public void Test_serialisation_of_valid_strings() {
    var str = new ValidString(nameof(JsonTests));
    var serialised = Json.Serialize(str);
    var deserialised = Json.Deserialize<ValidString>(serialised);
    Assert.That(str, Is.EqualTo(deserialised));
  }
  
  [Test] public void Test_serialisation_of_valid_string_subclasses() {
    var str = new CoreEntityId(nameof(JsonTests));
    var serialised = Json.Serialize(str);
    var deserialised = Json.Deserialize<CoreEntityId>(serialised);
    Assert.That(str, Is.EqualTo(deserialised));
  }

  [Test] public void Test_serialisation_of_objects_with_child_valid_strings() {
    var obj = new TestValidStrings { Str1 = new(nameof(TestValidStrings.Str1)), Str2 = new(nameof(TestValidStrings.Str2)) };
    var serialised = Json.Serialize(obj);
    var deserialised = Json.Deserialize<TestValidStrings>(serialised);
    Assert.That(obj, Is.EqualTo(deserialised));
  }
  
  [Test] public void Test_serialisation_of_objects_with_child_valid_string_subclasses() {
    var obj = new TestValidStringSubclasses { CoreId = new(nameof(TestValidStrings.Str1)), SystemId = new(nameof(TestValidStrings.Str2)) };
    var serialised = Json.Serialize(obj);
    var deserialised = Json.Deserialize<TestValidStringSubclasses>(serialised);
    Assert.That(obj, Is.EqualTo(deserialised));
  }
  
  [Test] public void Test_serialisation_of_object_dtos_with_child_valid_strings() {
    var obj = new TestValidStrings { Str1 = new(nameof(TestValidStrings.Str1)), Str2 = new(nameof(TestValidStrings.Str2)) };
    var serialised = Json.Serialize(obj);
    var deserialised = Json.Deserialize<TestValidStrings.Dto>(serialised).ToBase();
    Assert.That(obj, Is.EqualTo(deserialised));
  }
  
  [Test] public void Test_serialisation_of_object_dtos_with_child_valid_string_subclasses() {
    var obj = new TestValidStringSubclasses { CoreId = new(nameof(TestValidStrings.Str1)), SystemId = new(nameof(TestValidStrings.Str2)) };
    var serialised = Json.Serialize(obj);
    var deserialised = Json.Deserialize<TestValidStringSubclasses.Dto>(serialised).ToBase();
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
  
  [Test, Ignore("Json.Serialize does not handle ValidStrings without manual Dtos for now")] public void Test_Json_handles_ValidStrings_correctly_in_anon_objects() {
    var serialised = Json.Serialize(new { SystemId = new SystemEntityId("test")});
    Assert.That(serialised, Does.Not.Contain("Value"));
  }
  
  [Test] public async Task Test_serialisation_to_HttpContent_respects_interface_JsonIgnore() {
    var body = Json.SerializeToHttpContent(new SystemEntityType(Guid.NewGuid(), "Only this field should be serialised"));
    var json = await body.ReadAsStringAsync();
    Assert.That(json, Does.Contain("\"Prop\":"));
    Assert.That(json, Does.Contain("\"New Name\":"));
    Assert.That(json, Does.Contain("\"StandardProp\":"));
    Assert.That(json, Does.Not.Contain("\"JsonPropNameTest\":"));
    Assert.That(json, Does.Not.Contain("\"DisplayName\":"));
    Assert.That(json, Does.Not.Contain("\"SystemId\":"));
    Assert.That(json, Does.Not.Contain("\"LastUpdatedDate\":"));
    Assert.That(json, Does.Not.Contain("\"IgnoredProp\":"));
  }
  
  [Test] public void Test_without_Dtos_Nullability() {
    var validstrings = Json.Deserialize<TestValidStrings>(@"{ ""Str1"": ""1"", ""Str2"": ""2"" }");;
    Assert.That(validstrings, Is.EqualTo(new TestValidStrings {Str1 = new("1"), Str2 = new("2") }));
    
    var subclasses = Json.Deserialize<TestValidStringSubclasses>(@"{ ""CoreId"": ""1"", ""SystemId"": ""2"" }");;
    Assert.That(subclasses, Is.EqualTo(new TestValidStringSubclasses { CoreId = new("1"), SystemId = new("2") }));
    
    
    var obj = Json.Deserialize<ObjWithNullables>(@"{""NonNullStr"": ""str"", ""NonNullDt"": ""2020-01-01"", ""NonNullObj"": { ""Str1"": ""1"", ""Str2"": ""2""} }");
    Assert.That(obj, Is.EqualTo(new ObjWithNullables { NonNullStr = "str", NonNullDt = new DateTime(2020, 1, 1), NonNullObj = new TestValidStrings  {Str1 = new("1"), Str2 = new("2") }}));
    
    Assert.Throws<JsonException>(() => Json.Deserialize<ObjWithNullables>(@"{""NonNullDt"": ""2020-01-01"", ""NonNullObj"": { ""Str1"": ""1"", ""Str2"": ""2""} }"));
    Assert.Throws<JsonException>(() => Json.Deserialize<ObjWithNullables>(@"{""NonNullStr"": ""str"", ""NonNullObj"": { ""Str1"": ""1"", ""Str2"": ""2""} }"));
    Assert.Throws<JsonException>(() => Json.Deserialize<ObjWithNullables>(@"{""NonNullStr"": ""str"", ""NonNullDt"": ""2020-01-01""}"));
    Assert.Throws<JsonException>(() => Json.Deserialize<ObjWithNullables>(@"{""NonNullStr"": ""str"", ""NonNullDt"": ""2020-01-01"", ""NonNullObj"": { ""Str2"": ""2""} }"));
    Assert.Throws<JsonException>(() => Json.Deserialize<ObjWithNullables>(@"{""NonNullStr"": ""str"", ""NonNullDt"": ""2020-01-01"", ""NonNullObj"": { ""Str1"": ""1""} }"));
    
    Assert.Throws<JsonException>(() => Json.Deserialize<ObjWithNullables>("{}"));
    Assert.Throws<JsonException>(() => Json.Deserialize<ObjWithNullables>(@"{""NonNullStr"": ""str"", ""NonNullDt"": ""2020-01-01"", ""NonNullObj"": { } }"));
  }
  
  [Test] public void Test_without_Dtos_Data_Annotations() {
    // todo GT: implement
  }
  
  private void TestDtoImpl<T>(T baseobj) {
    ArgumentNullException.ThrowIfNull(baseobj);
    
    var json = Json.Serialize(baseobj);
    var deserialised = Json.Deserialize<T>(json); // should automatically detect the Dto 
    
    Assert.That(deserialised, Is.EqualTo(baseobj));
  }
  
  [Test] public void Test_SplitList_on_no_path() {
    var json = """[{"item":"1"}, {"item":"2"}]""";
    var list = Json.SplitList(json, String.Empty);
    Assert.That(list, Is.EquivalentTo(["""{"item":"1"}""", """{"item":"2"}"""]));
  }
  
  [Test] public void Test_SplitList_on_shallow_path() {
    var json = """{"path":[{"item":"1"}, {"item":"2"}]}""";
    var list = Json.SplitList(json, "path");
    Assert.That(list, Is.EquivalentTo(["""{"item":"1"}""", """{"item":"2"}"""]));
  }

  [Test] public void Test_SplitList_on_deeper_path() {
    var json = """{"path1": { "path2": [{"item":"1"}, {"item":"2"}]} }""";
    var list = Json.SplitList(json, "path1.path2");
    Assert.That(list, Is.EquivalentTo(["""{"item":"1"}""", """{"item":"2"}"""]));
  }
  
  [Test] public void Test_strongly_typed_SplitList() {
    var json = """{"path":[{"item":"1"}, {"item":"2"}]}""";
    var list = Json.SplitList<Row>(json, "path");
    Assert.That(list, Is.EquivalentTo([new Row("1"), new Row("2")]));
    Assert.That(list[0].item, Is.EqualTo("1"));
    Assert.That(list[1].item, Is.EqualTo("2"));
  } 
  
  record Row(string item);
 
  public record ObjWithNullables {
    public required string NonNullStr { get; init; }
    public string? NullStr { get; init; }
    
    public required DateTime NonNullDt { get; init; } = DateTime.MinValue;
    public DateTime? NullDt { get; init; }
    
    public required TestValidStrings NonNullObj { get; init; }
    public TestValidStrings? NullObj { get; init; }
  }
  
  public record TestValidStrings {
    public required ValidString Str1 { get; init; }
    public required ValidString Str2 { get; init; }
    
    public record Dto {
      public string? Str1 { get; init; }
      public string? Str2 { get; init; }
      
      public TestValidStrings ToBase() {
        return new TestValidStrings { 
          Str1 = new(Str1 ?? throw new Exception()), 
          Str2 = new(Str2 ?? throw new Exception()) };
      }
    }
  }
  
  public record TestValidStringSubclasses {
    public required CoreEntityId CoreId { get; init; }
    public required SystemEntityId SystemId { get; init; }
    
    public record Dto {
      public string? CoreId { get; init; }
      public string? SystemId { get; init; }
      
      public TestValidStringSubclasses ToBase() {
        return new TestValidStringSubclasses { 
          CoreId = new(CoreId ?? throw new Exception()), 
          SystemId = new(SystemId ?? throw new Exception()) };
      }
    }
  }
  
  public record BaseT {
    public ESystemStateStatus Status { get; set; }
    public required ValidString Str1 { get; set; }
    
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
  
  public record SystemEntityType(Guid Id, string Prop) : ISystemEntity {

    [JsonPropertyName("New Name")] public string JsonPropNameTest => nameof(JsonPropNameTest);
    public string StandardProp => nameof(JsonPropNameTest);
    [JsonIgnore] public bool IgnoredProp => true;
    
    // these three fields should be ignored during serialisation
    public string DisplayName => Prop;
    public SystemEntityId SystemId { get; } = new(Id.ToString());
    public CorrelationId CorrelationId { get; } = new(nameof(CorrelationId));
    public DateTime LastUpdatedDate { get; } = UtcDate.UtcNow;
    
    public ISystemEntity CreatedWithId(SystemEntityId newid) => this with { Id = Guid.Parse(newid.Value) };
    public object GetChecksumSubset() => throw new Exception();

  }
  
}