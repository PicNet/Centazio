using System.Reflection;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;

namespace Centazio.Core.Tests.Inspect;

public class CheckStandardNamingOfCommonTypes {
  
  private readonly Dictionary<Type, (string UpperCase, string LowerCase, bool EndsWith)> EXPECTED = new() {
    { typeof(CoreEntityTypeName), ("CoreEntityTypeName", "coretype", false) },
    { typeof(SystemEntityTypeName), ("SystemEntityTypeName", "systype", false) },
    { typeof(ObjectName), ("Object", "obj", false) },
    { typeof(SystemName), ("System", "system", false) },
    { typeof(LifecycleStage), ("Stage", "stage", false) },
    { typeof(CoreEntityChecksum), ("CoreEntityChecksum", "corchksm", false) },
    { typeof(SystemEntityChecksum), ("SystemEntityChecksum", "syschksm", false) },
    { typeof(CoreEntityId), ("CoreId", "coreid", true) },
    { typeof(SystemEntityId), ("SystemId", "systemid", true) },
    { typeof(ISystemEntity), ("SystemEntity", "sysent", true) },
    { typeof(ICoreEntity), ("CoreEntity", "coreent", true) },
  };
  
  private readonly List<Type> EXP_ORDER = [
    typeof(SystemName),
    typeof(SystemState),
    typeof(LifecycleStage), 
    typeof(ObjectName)
  ];

  [Test] public void Test_naming_standards() {
    var errors = new List<string>();
    InspectUtils.LoadCentazioAssemblies().ForEach(ValidateAssembly);

    void ValidateAssembly(Assembly ass) {
      ass.GetTypes().ForEach(ValidateType);

      void ValidateType(Type objtype) {
        if (objtype.Name.IndexOf("__", StringComparison.Ordinal) >= 0) return;
        var ifaces = objtype.GetInterfaces();
        if (Ignore(objtype) || ifaces.Any(Ignore)) { return; }
        
        var isrec = ReflectionUtils.IsRecord(objtype);
        
        objtype.GetConstructors().ForEach(ValidateCtor);
        objtype.GetProperties().ForEach(ValidateProp);
        objtype.GetFields().ForEach(ValidateField);
        
        var methods = isrec ? [] : objtype
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(m => m.Name.IndexOf('_', StringComparison.Ordinal) < 0)
            .ToList(); 
        methods.ForEach(ValidateMethod);
        methods.ForEach(ValidateMethodParamOrder);

        void ValidateCtor(ConstructorInfo ctor) {
          if (Ignore(ctor)) return;
          ctor.GetParameters().ForEach(p => ValidateParam("Ctor", p));
        }

        void ValidateProp(PropertyInfo prop) {
          if (Ignore(prop) || ifaces.Any(i => Ignore(i.GetProperty(prop.Name)))) return;
          ValidateImpl($"Property", prop.PropertyType, true, prop.Name);
        }

        void ValidateMethod(MethodInfo method) {
          if (Ignore(method)) return;
          method.GetParameters().ForEach(param => {
            if (String.IsNullOrWhiteSpace(param.Name) || param.GetCustomAttributes(typeof(IgnoreNamingConventionsAttribute), false).Length > 0) return;
            var imethods = ifaces.SelectMany(i => i.GetMethods().Where(m => m.Name == method.Name));
            var iparams = imethods.SelectMany(m => m.GetParameters().Where(p => p.Name == param.Name));
            if (iparams.Any(Ignore)) return;
            
            ValidateParam($"Method[{method.Name}]", param);
          });
        }
        
        void ValidateMethodParamOrder(MethodInfo method) {
          if (Ignore(method)) return;
          var args = method.GetParameters();
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

        void ValidateParam(string prefix, ParameterInfo param) {
          ValidateImpl(prefix + ".Param", param.ParameterType, isrec, param.Name ?? throw new Exception());
        }

        void ValidateImpl(string prefix, Type membertype, bool upper, string name) {
          var (type, plural) = GetTypeToTest();
          if (type is null || !EXPECTED.ContainsKey(type)) return;
          
          Check();
          
          void Check() {
            if (objtype == type) return; // do not check naming conventions if the object is the same type we are testing
            var checker = EXPECTED[type];
            var expected = upper ? checker.UpperCase : checker.LowerCase;
            if (plural) expected += "s";
            if (checker.EndsWith ? name.EndsWith(expected) : name == expected) return; // name is correct
            AddErr(prefix, type, $"Name[{name}] Expected[{expected}]");
          }
          
          (Type? TypeToTest, bool IsPlural) GetTypeToTest() {
            if (membertype.IsAssignableTo(typeof(System.Collections.IEnumerable))) {
              var args = membertype.GetGenericArguments();
              if (args.Length == 1) return (args.SingleOrDefault(), true);
            }
            return (membertype, false);
          }
        }
        
        void AddErr(string prefix, Type type, string suffix) => errors.Add($"Assembly[{ass.GetName().Name}] Object[{objtype.Name}] Record[{isrec}] {prefix} - Type[{type.Name}] {suffix}");
      }
    }

    Assert.That(errors, Is.Empty, "\n\n" + String.Join("\n", errors) + "\n\n\n\n----------------------------------------------\n");
  }

  private bool Ignore(ICustomAttributeProvider? prov) => 
      prov is not null && prov.GetCustomAttributes(typeof(IgnoreNamingConventionsAttribute), false).Length > 0;

}