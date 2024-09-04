using System.Data;
using System.Diagnostics;
using Centazio.Core;
using Dapper;

namespace Centazio.Providers.SqlServer;

public class DapperInitialiser {

  public static void Initialise() {
    SqlMapper.AddTypeHandler(new ValidStringSqlTypeHandler<SystemName>());
    SqlMapper.AddTypeHandler(new ValidStringSqlTypeHandler<ObjectName>());
    SqlMapper.AddTypeHandler(new ValidStringSqlTypeHandler<LifecycleStage>());
    SqlMapper.AddTypeHandler(new DateTimeSqlTypeHandler());
    
    SqlMapper.AddTypeMap(typeof(DateTime), DbType.DateTime2);
  }

  private class DateTimeSqlTypeHandler : SqlMapper.TypeHandler<DateTime> {
    public override void SetValue(IDbDataParameter parameter, DateTime value) { parameter.Value = value; }
    public override DateTime Parse(object value) { return DateTime.SpecifyKind((DateTime) value, DateTimeKind.Utc); }
  }
  
  private class ValidStringSqlTypeHandler<T> : SqlMapper.TypeHandler<T> where T : ValidString {
    public override void SetValue(IDbDataParameter parameter, T? value) => 
        parameter.Value = value?.Value ?? throw new Exception($"{nameof(value)} must ne non-empty");

    public override T Parse(object? value) {
      ArgumentException.ThrowIfNullOrWhiteSpace((string?) value);
      return (T?) Activator.CreateInstance(typeof(T), (string?) value) ?? throw new UnreachableException();
    }
  }
}