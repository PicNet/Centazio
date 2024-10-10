﻿using System.Reflection;
using Centazio.Core.Checksum;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;

namespace Centazio.Core.Tests.Inspect;

public class CheckStandardNamingOfCommonTypes {
  
  private readonly Dictionary<Type, (string Uppercase, string Lowercase)> EXPECTED = new() {
    { typeof(CoreEntityType), (nameof(CoreEntityType), "coretype") },
    { typeof(SystemEntityType), (nameof(SystemEntityType), "systype") },
    { typeof(ObjectName), (nameof(ObjectState.Object), "obj") },
    { typeof(SystemName), (nameof(ObjectState.System), "system") },
    { typeof(LifecycleStage), (nameof(SystemState.Stage), "stage") },
    { typeof(CoreEntityChecksum), (nameof(CoreEntityChecksum), "corchksm") },
    { typeof(SystemEntityChecksum), (nameof(SystemEntityChecksum), "syschksm") },
    { typeof(CoreEntityId), (nameof(Map.CoreToSystem.CoreId), "coreid") },
    { typeof(SystemEntityId), (nameof(Map.CoreToSystem.SystemId), "systemid") },
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
          if (Ignore(prop)) return;
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

        void ValidateImpl(string prefix, Type type, bool upper, string name) {
          EXPECTED.TryGetValue(type, out var check);
          if (check.Uppercase is not null) Check(type);
          
          void Check(Type exp) {
            if (objtype == exp) return; // do not check naming conventions if the object is the same type we are testing
            var expected = upper ? check.Uppercase : check.Lowercase;
            if (name == expected) return; // name is correct
            AddErr(prefix, type, $"Name[{name}] Expected[{expected}]");
          }
        }
        
        void AddErr(string prefix, Type type, string suffix) => errors.Add($"Assembly[{ass.GetName().Name}] Object[{objtype.Name}] Record[{isrec}] {prefix} - Type[{type.Name}] {suffix}");
      }
    }

    Assert.That(errors, Is.Empty, "\n\n" + String.Join("\n", errors) + "\n\n\n\n----------------------------------------------\n");
  }

  private bool Ignore(ICustomAttributeProvider prov) => 
      prov.GetCustomAttributes(typeof(IgnoreNamingConventionsAttribute), false).Length > 0;

}