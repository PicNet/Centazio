﻿using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Centazio.Core.Misc;

namespace Centazio.Cli.Commands.Dev;

public class GenerateSettingTypesCommand(ITemplater templater) : AbstractCentazioCommand<CommonSettings> {
  
  public override Task<CommonSettings> GetInteractiveSettings() => Task.FromResult(new CommonSettings());

  public override async Task ExecuteImpl(CommonSettings cmdsetts) {
    var dir = FsUtils.GetSolutionFilePath("src", "Centazio.Core", "Settings");
    var schema = JsonNode.Parse(Json.ReadFile(Path.Combine(dir, "settings_schema.json"))) ?? throw new Exception();

    var sb = new StringBuilder();
    new SettingsClassGenerator(schema.AsObject(), sb, templater).GenerateClasses();

    await File.WriteAllTextAsync(Path.Combine(dir, "SettingsTypes.cs"), sb.ToString());
  }

  public class SettingsClassGenerator(JsonObject schema, StringBuilder sb, ITemplater templater) {

    private readonly string HEADER = @"

///////////////////////////////////////////////////////////////////////////////
// Note: this file is generated by `centazio dev gen-settings` - DO NOT MODIFY
///////////////////////////////////////////////////////////////////////////////

namespace Centazio.Core.Settings;

"; 
   
    private readonly string RECORD_TEMPLATE = @"
public record {{ it.ClassName }} {
{{ for field in it.Fields }}
  {{ field.PropertyDefenition }}{{ end }}

  public Dto ToDto() => new() { {{ for field in it.Fields }}
    {{ field.ToDtoPropertyPair }}{{ end }}
  };

  public record Dto : IDto<{{it.ClassName}}> { {{ for field in it.Fields }}
    {{ field.DtoPropertyDefenition }}{{ end }}

    public {{ it.ClassName }} ToBase() => new() { {{ for field in it.Fields }}
      {{ field.ToBasePropertyPair }}{{ end }}
    };
  }
}";
    
    public void GenerateClasses() {
      sb.Append(HEADER);
      schema.Select(p => new FieldSpec(p, String.Empty))
          .Where(n => n.Value.GetValueKind() == JsonValueKind.Object)
          .ForEach(prop => GenerateClassAndNestedTypes(prop.SettingsName, prop.Value.AsObject().ToList(), new HashSet<string>()));
    }

    private void GenerateClassAndNestedTypes(string classnm, List<KeyValuePair<string, JsonNode?>> props, HashSet<string> generated) {
      if (!generated.Add(classnm)) return;

      var fields = props.Select(prop => new FieldSpec(prop, classnm)).ToList();
      
      fields.Where(f => f.IsObj).ForEach(obj => GenerateClassAndNestedTypes(obj.SettingsName, obj.Value.AsObject().ToList(), generated));
      fields.Where(f => f.IsArray).ForEach(arr => GenerateClassAndNestedTypes(arr.ElementTypeName, arr.ElementTypeProps.ToList(), generated));
      
      sb.AppendLine(templater.ParseFromContent(RECORD_TEMPLATE, new { ClassName = classnm, Fields = fields }));
    }
  }

  public class FieldSpec {
    
    public string ClassName { get; }
    public string Name { get; }
    public JsonNode Value { get; }
    
    public bool Required { get; }
    public string Type { get; }
    
    public FieldSpec(KeyValuePair<string, JsonNode?> prop, string classnm) {
      (ClassName, Name, Value) = (classnm, prop.Key, prop.Value ?? throw new Exception());

      if (Value.GetValueKind() == JsonValueKind.Object) {
        var obj = Value.AsObject();
        if (IsFieldSpec(obj)) {
          Type = obj["type"]?.GetValue<string>() ?? "string";
          Required = obj["required"]?.GetValue<bool>() ?? true;
        } else {
          Type = "object";
          Required = true;
        }
      } else if (IsArray && Value.AsArray().Count > 0) {
        Type = $"List<{Name}Settings.Dto>";
        Required = true;
      } else {
        Type = "string";
        Required = true;
      }
    }
    
    public bool IsArray => Value.GetValueKind() == JsonValueKind.Array;
    public bool IsObj => Value.GetValueKind() == JsonValueKind.Object && !IsFieldSpec(Value.AsObject());
    
    public string SettingsName => Char.ToUpper(Name[0]) + Name[1..] + "Settings";
    public string ElementTypeName => Char.ToUpper(Name[0]) + Name[1..] + "Settings";

    public List<KeyValuePair<string, JsonNode?>> ElementTypeProps { get {
      if (!IsArray || Value.AsArray().Count == 0) return [];
      var first = Value.AsArray()[0];
      return first?.GetValueKind() == JsonValueKind.Object ? first.AsObject().ToList() : [];
    } }

  
    public string PropertyDefenition { get {
      var requiredmod = Required ? "required " : string.Empty;
      var opt = Required ? String.Empty : "?";
      return IsArray 
          ? $"public {requiredmod}List<{ElementTypeName}>{opt} {Name} {{ get; init; }}" 
          : IsObj 
              ? $"public {requiredmod}{SettingsName}{opt} {Name} {{ get; init; }}" 
              : $"public {requiredmod}{Type}{opt} {Name} {{ get; init; }}";
    } }
    
    public string DtoPropertyDefenition => IsObj 
        ? $"public {SettingsName}.Dto? {Name} {{ get; init; }}" 
        : $"public {Type}? {Name} {{ get; init; }}";

    public string ToDtoPropertyPair { get {
      if (IsArray) { return $"{Name} = {Name}?.Select(item => item.ToDto()).ToList(),"; }
      if (IsObj) return $"{Name} = {Name}.ToDto(),";
      return $"{Name} = {Name},";
    }}
    
    public string ToBasePropertyPair { get {
      if (IsArray) return $"{Name} = {Name}?.Select(dto => dto.ToBase()).ToList() ?? new List<{ElementTypeName}>(),";
      if (IsObj) return Required ? $"{Name} = {Name}?.ToBase() ?? throw new ArgumentNullException(nameof({Name}))," : $"      {Name} = {Name}?.ToBase() ?? new(),";
      if (Type == "string") return Required ? $"{Name} = String.IsNullOrWhiteSpace({Name}) ? throw new ArgumentNullException(nameof({Name})) : {Name}.Trim()," : $"      {Name} = {Name}?.Trim(),";
      
      return $"{Name} = {Name} ?? {GetDefaultValue()},";
    } }
    
    private bool IsFieldSpec(JsonObject obj) => obj.ContainsKey("type") || obj.ContainsKey("required") || obj.ContainsKey("default");

    private string GetDefaultValue() => Type.ToLower() switch {
      "bool" => "false",
      "int" => "0",
      _ => "null"
    };

  }

}