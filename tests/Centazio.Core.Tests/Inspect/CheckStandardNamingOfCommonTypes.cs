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
  };

  [Test] public void Test_naming_standards() {
    var errors = new List<string>();
    InspectUtils.LoadCentazioAssemblies().ForEach(ValidateAssembly);

    void ValidateAssembly(Assembly ass) {
      ass.GetTypes().ForEach(ValidateType);

      void ValidateType(Type objtype) {
        if (objtype.Name.IndexOf("__", StringComparison.Ordinal) >= 0) return;
        var ifaces = objtype.GetInterfaces();
        if (Ignore(objtype) || ifaces.Any(Ignore)) return;
        
        var isrec = ReflectionUtils.IsRecord(objtype);
        
        objtype.GetConstructors().ForEach(ValidateCtor);
        objtype.GetProperties().ForEach(ValidateProp);
        objtype.GetFields().ForEach(ValidateField);
        if (!isrec) objtype.GetMethods().ForEach(ValidateMethod);

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
          var args = method.GetParameters();  
          args.ForEach(param => {
            if (param.GetCustomAttributes(typeof(IgnoreNamingConventionsAttribute), false).Length > 0) return;
            var imethods = ifaces.SelectMany(i => i.GetMethods().Where(m => m.Name == method.Name));
            var iparams = imethods.SelectMany(m => m.GetParameters().Where(p => p.Name == param.Name));
            if (iparams.Any(Ignore)) return;
            
            ValidateParam($"Method[{method.Name}]", param);
          });
        }
        
        void ValidateField(FieldInfo field) {
          if (Ignore(field)) return;
          ValidateImpl($"Field", field.FieldType, true, field.Name);
        }

        void ValidateParam(string prefix, ParameterInfo param) {
          ValidateImpl(prefix + ".Param", param.ParameterType, isrec, param.Name ?? throw new Exception(), param.Position);
        }

        void ValidateImpl(string prefix, Type type, bool upper, string name, int? position = null) {
          Check(typeof(CoreEntityType), name);
          Check(typeof(SystemEntityType), name);
          Check(typeof(ObjectName), name);
          Check(typeof(SystemName), name);
          Check(typeof(LifecycleStage), name);
          Check(typeof(CoreEntityChecksum), name);
          Check(typeof(SystemEntityChecksum), name);
          
          void Check(Type exp, string actual) {
            if (objtype == exp) return; // do not check naming conventions if the object is the same type we are testing
            if (type != exp) return; // only test properties, fields, params if they are of the type we are testing
            var expected = upper ? EXPECTED[type].Uppercase : EXPECTED[type].Lowercase;
            if (actual == expected) return; // name is correct
            errors.Add($"Assembly[{ass.GetName().Name}] Object[{objtype.Name}] Record[{isrec}] {prefix} - Type[{type.Name}] Name[{actual}] Expected[{expected}] Pos[{position}]");
          }
        }
      }
    }

    Assert.That(errors, Is.Empty, "\n\n" + String.Join("\n", errors) + "\n\n\n\n----------------------------------------------\n");
  }

  private bool Ignore(ICustomAttributeProvider prov) => 
      prov.GetCustomAttributes(typeof(IgnoreNamingConventionsAttribute), false).Length > 0;

}