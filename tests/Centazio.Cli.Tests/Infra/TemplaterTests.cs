using Centazio.Core.Settings;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests.Infra;

public class TemplaterTests {

  private readonly CentazioSettings settings = TestingFactories.Settings();
  private readonly ITemplater templater = new Templater(TestingFactories.Settings(), TestingFactories.Secrets());
  
  [Test] public void Test_parse_simple_property() {
      var contents = @"
Prop1:{{  it.Prop1 }}
";
    var results = templater.ParseFromContent(contents, new { 
      Prop1 = "P1" 
    });
    Assert.That(results.Trim(), Is.EqualTo("Prop1:P1"));
  }
  
  [Test] public void Test_parse_complex_object() {
    var contents = @"
Prop1:{{it.Prop1}}
Obj.Prop2:{{ it.Obj.Prop2}}
Arr[0]:{{it.Arr[0].Prop}}
Arr[1]:{{it.Arr[1].Prop }}
Arr2[0]:{{ it.Arr2[0] }}
Arr2[1]:{{ it.Arr2[1] }}
";
    var results = templater.ParseFromContent(contents, new { 
      Prop1 = "P1", 
      Obj = new { Prop2 = "P2" }, 
      Arr = new [] { new { Prop = "Arr.P0" }, new { Prop = "Arr.P1" } }, 
      Arr2 = new [] { 1, 2 } 
    });
    Assert.That(results.Replace("\r", String.Empty).Trim(), Is.EqualTo(String.Join('\n', new [] {"Prop1:P1", "Obj.Prop2:P2", "Arr[0]:Arr.P0", "Arr[1]:Arr.P1", "Arr2[0]:1", "Arr2[1]:2" })));
  }

  [Test] public void Test_parse_with_no_object_and_settings() {
    var contents = @"
settings.SecretsFolders[0]:{{ it.settings.SecretsFolders[0] }}
";
    var results = templater.ParseFromContent(contents);
    Assert.That(results.Trim(), Is.EqualTo($"settings.SecretsFolders[0]:{settings.SecretsFolders[0]}"));
  }
  
  [Test] public void Test_parse_with_object_and_settings() {
    var contents = @"
Prop1:{{it.Prop1}}
settings.SecretsFolders[0]:{{ it.settings.SecretsFolders[0] }}
";
    var results = templater.ParseFromContent(contents, new { 
      Prop1 = "P1", 
    });
    Assert.That(results.Replace("\r", String.Empty).Trim(), Is.EqualTo(String.Join('\n', new [] {"Prop1:P1", $"settings.SecretsFolders[0]:{settings.SecretsFolders[0]}" })));
  }
}