using Centazio.Core.Misc;

namespace Centazio.Core.Tests.Misc;

public class JsonTests {

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
  } 
  
  record Row(string item);
  
}