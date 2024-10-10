using System.Data;
using System.Diagnostics;
using Centazio.Core;
using Dapper;

namespace Centazio.Providers.SqlServer;

public class DapperInitialiser {

  public static void Initialise() {
    AddAllRequiredValidStringSqlHandlers();
    SqlMapper.AddTypeHandler(new DateTimeSqlTypeHandler());
    SqlMapper.AddTypeMap(typeof(DateTime), DbType.DateTime2);
  }

  private static void AddAllRequiredValidStringSqlHandlers() {
    var handler = new ValidStringSqlTypeHandler();
    typeof(ValidString).Assembly.GetTypes()
        .Where(t => !t.IsAbstract && t.IsAssignableTo(typeof(ValidString)))
        .ForEach(t => SqlMapper.AddTypeHandler(t, handler));
  }

  private class DateTimeSqlTypeHandler : SqlMapper.TypeHandler<DateTime> {
    public override void SetValue(IDbDataParameter parameter, DateTime value) { parameter.Value = value; }
    public override DateTime Parse(object value) { return DateTime.SpecifyKind((DateTime) value, DateTimeKind.Utc); }
  }
  
  private class ValidStringSqlTypeHandler : SqlMapper.ITypeHandler {

    public void SetValue(IDbDataParameter parameter, object value) { 
      parameter.Value = ((ValidString?)value)?.Value ?? throw new Exception($"{nameof(value)} must ne non-empty");
    }
    public object Parse(Type destinationType, object value) {
      ArgumentException.ThrowIfNullOrWhiteSpace((string?) value);
      return Activator.CreateInstance(destinationType, (string?) value) ?? throw new UnreachableException();
    }

  }
}