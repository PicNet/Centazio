using Centazio.Core.Checksum;
using Centazio.Core.Ctl;

namespace Centazio.Core.Tests.Inspect;

public class CheckStandardNamingOfCommonTypes {
  
  private readonly Dictionary<Type, (string UpperCase, string LowerCase, bool EndsWith)> EXPECTED = new() {
    { typeof(CoreEntityTypeName), ("CoreEntityTypeName", "coretype", false) },
    { typeof(SystemEntityTypeName), ("SystemEntityTypeName", "systype", false) },
    { typeof(ObjectName), ("Object", "obj", false) },
    { typeof(SystemName), ("System", "system", true) },
    { typeof(LifecycleStage), ("Stage", "stage", false) },
    { typeof(CoreEntityChecksum), ("CoreEntityChecksum", "corchksm", true) },
    { typeof(SystemEntityChecksum), ("SystemEntityChecksum", "syschksm", true) },
    { typeof(CoreEntityId), ("CoreId", "coreid", true) },
    { typeof(SystemEntityId), ("SystemId", "systemid", true) },
    { typeof(ISystemEntity), ("SystemEntity", "sysent", true) },
    { typeof(ICoreEntity), ("CoreEntity", "coreent", true) },
    { typeof(ICtlRepository), ("CtlRepo", "ctl", false) },
  };
  
  private readonly List<Type> EXP_ORDER = [
    typeof(SystemName),
    typeof(SystemState),
    typeof(LifecycleStage), 
    typeof(ObjectName)
  ];
  
  private readonly List<string> IGNORE = ["Centazio.Sample"];
  
  [Test] public void Test_naming_standards() {
    var errors = new List<string>();
    InspectUtils.LoadCentazioAssemblies().ForEach(ValidateAssembly);

    void ValidateAssembly(Assembly ass) {
      if (IGNORE.Contains(ass.GetName().Name, StringComparer.Ordinal)) return;
      ass.GetExportedTypes().ForEach(ValidateType);

      void ValidateType(Type objtype) {
        if (objtype.Name.IndexOf("__", StringComparison.Ordinal) >= 0 || Ignore(objtype)) return;

        var ifaces = objtype.GetInterfaces();
        var isrec = ReflectionUtils.IsRecord(objtype);
        
        objtype.GetConstructors().ForEach(ValidateCtor);
        GetDistinctByPropertyType(objtype.GetProperties()).ForEach(ValidateProp);
        objtype.GetFields().ForEach(ValidateField);
        
        var methods = isrec ? [] : objtype
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(m => m.Name.IndexOf('_', StringComparison.Ordinal) < 0)
            .ToList(); 
        methods.ForEach(ValidateMethod);
        methods.ForEach(ValidateMethodParamOrder);

        void ValidateCtor(ConstructorInfo ctor) {
          if (Ignore(ctor)) return;
          GetParamsSafe(ctor).ForEach(p => ValidateParam("Ctor", p, true));
        }

        void ValidateProp(PropertyInfo prop) {
          if (Ignore(prop) || ifaces.Any(i => Ignore(i.GetProperty(prop.Name)))) return;
          ValidateImpl($"Property", prop.PropertyType, true, prop.Name);
        }

        void ValidateMethod(MethodInfo method) {
          if (Ignore(method)) return;
          GetParamsSafe(method).ForEach(param => {
            if (String.IsNullOrWhiteSpace(param.Name) || param.GetCustomAttributes(typeof(IgnoreNamingConventionsAttribute), false).Length > 0) return;
            var imethods = ifaces.SelectMany(i => i.GetMethods().Where(m => m.Name == method.Name));
            var iparams = imethods.SelectMany(m => m.GetParameters().Where(p => p.Name == param.Name));
            if (iparams.Any(Ignore)) return;
            
            ValidateParam($"Method[{method.Name}]", param, false);
          });
        }
        
        void ValidateMethodParamOrder(MethodInfo method) {
          if (Ignore(method)) return;
          var args = GetParamsSafe(method);
          var ordered = EXP_ORDER.Where(t => args.Any(p => t.IsAssignableFrom(p.ParameterType))).ToList();
          ordered.ForEach((t, exp) => {
            var p = args.Single(p => t.IsAssignableFrom(p.ParameterType));
            if (p.Position != exp) AddErr($"Method[{method.Name}]", p.ParameterType, $"Name[{p.Name}] Position[{p.Position}] Expected[{exp}]");
          });
        }
        
        void ValidateField(FieldInfo field) {
          if (Ignore(field)) return;
          ValidateImpl($"Field", field.FieldType, true, field.Name);
        }

        void ValidateParam(string prefix, ParameterInfo param, bool isctor) {
          var name = param.Name ?? throw new Exception();
          var isRecordPrimaryCtorParam = isrec && isctor && objtype.GetProperty(name) is not null;
          ValidateImpl(prefix + ".Param", param.ParameterType, isRecordPrimaryCtorParam, name);
        }

        void ValidateImpl(string prefix, Type membertype, bool upper, string name) {
          var (type, plural) = GetTypeToTest();
          if (type is null || name.IndexOf("__", StringComparison.Ordinal) >= 0 || name.IndexOf("<>", StringComparison.Ordinal) >= 0) return;
          if (!EXPECTED.ContainsKey(type)) {
            CheckCammelCaseUsage();
            return;
          }
          
          Check();

          (Type? TypeToTest, bool IsPlural) GetTypeToTest() {
            if (membertype.IsAssignableTo(typeof(System.Collections.IEnumerable))) {
              var args = membertype.GetGenericArguments();
              if (args.Length == 1) return (args.SingleOrDefault(), true);
            }
            return (membertype, false);
          }
          
          void CheckCammelCaseUsage() {
            var totest = name.StartsWith('_') ? name.Substring(1) : name;
            if (totest.Length == 0) return;
            if (upper && !Char.IsUpper(totest[0])) AddErr(prefix, type, $"Name[{name}] should be upper cammel-case");
            if (!upper && Char.IsUpper(totest[0])) AddErr(prefix, type, $"Name[{name}] should be lower cammel-case");
          }

          void Check() {
            if (objtype == type) return; // do not check naming conventions if the object is the same type we are testing
            var checker = EXPECTED[type];
            var expected = upper ? checker.UpperCase : checker.LowerCase;
            if (plural) expected += "s";
            if (checker.EndsWith ? name.EndsWith(expected) : name == expected) return; // name is correct
            AddErr(prefix, type, $"Name[{name}] Expected[{expected}]");
          }
        }
        
        void AddErr(string prefix, Type type, string suffix) => errors.Add($"Assembly[{ass.GetName().Name}] Object[{objtype.Name}] Record[{isrec}] {prefix} - Type[{type.Name}] {suffix}");
      }
    }

    Assert.That(errors, Is.Empty, "\n\n" + String.Join("\n", errors) + "\n\n\n\n----------------------------------------------\n");

    // ignores methods that throw Reflection exceptions due to references to some nuget packages
    // also returns only a single (first) parameter of a given type, as subsequent parameters cannot have the 'expected' name
    List<ParameterInfo> GetParamsSafe(MethodBase method) {
      try { 
        var args = method.GetParameters();
        var done = new Dictionary<Type, bool>();
        return args.Where(a => !EXPECTED.ContainsKey(a.ParameterType) || done.TryAdd(a.ParameterType, true)).ToList();
      } catch (Exception) { return []; }
    }
    
    List<PropertyInfo> GetDistinctByPropertyType(PropertyInfo[] props) {
      var done = new Dictionary<Type, bool>();
      return props.Where(a => !EXPECTED.ContainsKey(a.PropertyType) || done.TryAdd(a.PropertyType, true)).ToList();
    }
  }

  private bool Ignore(ICustomAttributeProvider? prov) {
    if (prov is null) return false;
    if (prov.GetCustomAttributes(typeof(IgnoreNamingConventionsAttribute), false).Length > 0) return true;
    var ifaces = (prov as Type)?.GetInterfaces() ?? [];
    if (ifaces.Any(Ignore)) return true;
    return Ignore((prov as Type)?.DeclaringType);
  }

}