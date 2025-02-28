using System.Dynamic;
using Centazio.Core.Misc;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Scriban;

namespace Centazio.Cli.Infra;

public interface ITemplater {
  string ParseFromPath(string path, object? model = null);
  string ParseFromContent(string content, object? model = null);
}
public class Templater(CentazioSettings settings, CentazioSecrets secrets) : ITemplater {
  
  public string ParseFromPath(string path, object? model = null) => 
      ParseFromContent(File.ReadAllText(FsUtils.GetSolutionFilePath("defaults", "templates", path)), model);

  public string ParseFromContent(string contents, object? model = null) {
    var dyn = ToDynamic(model);
    (dyn.settings, dyn.secrets) = (settings, secrets);
    return Template.Parse(contents.Replace("{{", "{{Model.")).Render(new { Model=dyn }, m => m.Name);
  }

  private static dynamic ToDynamic(object? obj) {
    if (obj is null) return new ExpandoObject();
    var dyn = new ExpandoObject() as IDictionary<string, object?>;
    obj.GetType().GetProperties().ForEach(p => dyn.Add(p.Name, p.GetValue(obj)));
    return dyn;
  }

}