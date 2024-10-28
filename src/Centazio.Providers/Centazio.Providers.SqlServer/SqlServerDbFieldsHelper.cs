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
        f.type == typeof(int) ? "int" : 
        f.type == typeof(decimal) ? "decimal" : 
        f.type == typeof(DateTime) ? "datetime2" : 
        f.type == typeof(DateOnly) ? "date" : 
        f.type == typeof(Boolean) ? "bit" : 
        f.type == typeof(Guid) ? "uniqueidentifier" : 
        f.type == typeof(string) ? "nvarchar" : 
        throw new NotSupportedException(f.type.Name);
    if (!String.IsNullOrWhiteSpace(f.length)) typestr += $"({f.length})";
    var nullstr = f.required ? "not null" : "null";
    return $"[{f.name}] {typestr} {nullstr}";
  }

  public override string GenerateDropTableScript(string schema, string table) =>  $"DROP TABLE IF EXISTS {TableName(schema, table)}";
  public override string TableName(string schema, string table) => $"[{schema}].[{table}]";

  public override string GenerateIndexScript(string schema, string table, params string[] columns) {
    var name = $"ix_{table}_{String.Join("_", columns.Select(c => c.ToLower()))}";
    return $"DROP INDEX IF EXISTS {name} ON {TableName(schema, table)};\nCREATE INDEX {name} ON {TableName(schema, table)} ({String.Join(", ", columns)});";
  }

}