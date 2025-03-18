using System.Text;
using Centazio.Core.Misc;

namespace Centazio.Providers.SqlServer;

public class SqlServerDbFieldsHelper : AbstractDbFieldsHelper {
  
  public override string GenerateCreateTableScript(string schema, string table, List<DbFieldType> fields, string[] pkfields, string? additional=null) {
    var sql = new StringBuilder();
    var additionaltxt = String.IsNullOrWhiteSpace(additional) ? String.Empty : ",\n    " + additional;
    if (!String.IsNullOrWhiteSpace(schema)) {
      sql.AppendLine($@"
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'{schema}')
  EXEC('CREATE SCHEMA [{schema}] AUTHORIZATION [dbo]');");
    }
    sql.AppendLine($@"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{table}' AND xtype='U')
BEGIN
  CREATE TABLE {TableName(schema, table)} (
    {String.Join(",\n    ", fields.Select(GetDbFieldTypeString))},
    PRIMARY KEY ({String.Join(", ", pkfields)}){additionaltxt}
  )
END
");
    return sql.ToString().Trim();
  }

  private string GetDbFieldTypeString(DbFieldType f) {
    var typestr = 
        f.FieldType == typeof(int) ? "int" : 
        f.FieldType == typeof(decimal) ? "decimal" : 
        f.FieldType == typeof(DateTime) ? "datetime2" : 
        f.FieldType == typeof(DateOnly) ? "date" : 
        f.FieldType == typeof(Boolean) ? "bit" : 
        f.FieldType == typeof(Guid) ? "uniqueidentifier" : 
        f.FieldType == typeof(string) ? "nvarchar" : 
        throw new NotSupportedException(f.FieldType.Name);
    if (!String.IsNullOrWhiteSpace(f.Length)) typestr += $"({f.Length})";
    var nullstr = f.Required ? "not null" : "null";
    return $"[{f.Name}] {typestr} {nullstr}";
  }

  public override string GenerateDropTableScript(string schema, string table) =>  $"DROP TABLE IF EXISTS {TableName(schema, table)}";
  public override string TableName(string schema, string table) => $"[{schema}].[{table}]";

  public override string GenerateIndexScript(string schema, string table, params List<string> columns) {
    var name = $"ix_{table}_{String.Join("_", columns.Select(c => c.ToLower()))}";
    return $"DROP INDEX IF EXISTS {name} ON {TableName(schema, table)};\nCREATE INDEX {name} ON {TableName(schema, table)} ({String.Join(", ", columns)});";
  }

}