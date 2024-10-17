using System.Data;
using System.Diagnostics;
using Centazio.Core;
using Dapper;

namespace Centazio.Providers.Sqlite;

public class DapperInitialiser {

  public static void Initialise() {
    AddAllRequiredValidStringSqlHandlers();
    SqlMapper.AddTypeHandler(new DateTimeHandler());
    SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
    SqlMapper.AddTypeHandler(new GuidHandler());
    SqlMapper.AddTypeHandler(new TimeSpanHandler());
  }

  private static void AddAllRequiredValidStringSqlHandlers() {
    var handler = new ValidStringSqlTypeHandler();
    ValidString.AllSubclasses().ForEach(t => SqlMapper.AddTypeHandler(t, handler));
  }

  private abstract class SqliteTypeHandler<T> : SqlMapper.TypeHandler<T> {
    public override void SetValue(IDbDataParameter parameter, T? value) => parameter.Value = value;
  }

  private class DateTimeHandler : SqliteTypeHandler<DateTime> {
    public override DateTime Parse(object value) => DateTime.Parse((string)value).ToUniversalTime();
  }
  
  private class DateTimeOffsetHandler : SqliteTypeHandler<DateTimeOffset> {
    public override DateTimeOffset Parse(object value) => DateTimeOffset.Parse((string)value);
  }

  private class GuidHandler : SqliteTypeHandler<Guid> {
    public override Guid Parse(object value) => Guid.Parse((string)value);
  }

  private class TimeSpanHandler : SqliteTypeHandler<TimeSpan> {
    public override TimeSpan Parse(object value) => TimeSpan.Parse((string)value);
  }

  private class ValidStringSqlTypeHandler : SqlMapper.ITypeHandler {

    public void SetValue(IDbDataParameter parameter, object value) { 
      parameter.Value = ((ValidString?)value)?.Value ?? throw new Exception($"{nameof(value)} must ne non-empty");
    }
    
    public object Parse(Type type, object value) {
      ArgumentException.ThrowIfNullOrWhiteSpace((string?) value);
      return Activator.CreateInstance(type, (string?) value) ?? throw new UnreachableException();
    }

  }
}