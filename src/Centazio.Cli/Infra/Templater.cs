using System.Dynamic;
using System.Reflection;
using System.Text.Json.Serialization;
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
  
  internal static string TestingRootDir = String.Empty;
  
  /// <summary>
  /// This method will work when running using the centazio 'dotnet tool' and when running within
  ///     the Centazio development hierarchy
  /// </summary>
  public static string TemplatePath(params List<string> steps) {
    return Path.Combine(steps.ToList().Prepend(RootDir()).ToArray());
    
    string RootDir() {
      try { return FsUtils.GetSolutionRootDirectory(); }
      catch (Exception) { 
        return !String.IsNullOrEmpty(TestingRootDir) 
          ? TestingRootDir 
          :  Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) 
          ?? throw new Exception(); 
      }
    }
  }
  
  public string ParseFromPath(string path, object? model = null) => 
      ParseFromContent(File.ReadAllText(FsUtils.GetSolutionFilePath("defaults", "templates", path)), model);

  public string ParseFromContent(string contents, object? model = null) {
    var dyn = ToDynamic(model);
    (dyn.settings, dyn.secrets) = (settings, secrets);
    return Template.Parse(contents).Render(new { it=dyn }, m => m.Name);
  }

  private static dynamic ToDynamic(object? obj) {
    if (obj is null) return new ExpandoObject();
    var dyn = new ExpandoObject() as IDictionary<string, object?>;
    obj.GetType().GetProperties().Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() is null).ForEach(p => dyn.Add(p.Name, p.GetValue(obj)));
    return dyn;
  }

}