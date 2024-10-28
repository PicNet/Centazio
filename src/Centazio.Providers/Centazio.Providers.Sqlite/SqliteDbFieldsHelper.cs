using Centazio.Core.Misc;

namespace Centazio.Providers.Sqlite;

public class SqliteDbFieldsHelper : AbstractDbFieldsHelper {

  public override string GenerateCreateTableScript(string schema, string table, List<DbFieldType> fields, string[] pkfields, string? additional=null) {
    var additionaltxt = String.IsNullOrWhiteSpace(additional) ? String.Empty : ",\n  " + additional;
    return $@"CREATE TABLE IF NOT EXISTS {TableName(schema, table)} (
  {String.Join(",\n    ", fields.Select(GetDbFieldTypeString))},
  PRIMARY KEY ({String.Join(", ", pkfields)}){additionaltxt})";
  }

  public override string GenerateIndexScript(string schema, string table, params string[] columns) => 
      $"CREATE INDEX IF NOT EXISTS ix_{table}_{String.Join("_", columns.Select(c => c.ToLower()))} ON {TableName(schema, table)} ({String.Join(", ", columns)});";

  public override string GenerateDropTableScript(string schema, string table) =>  $"DROP TABLE IF EXISTS {TableName(schema, table)}";
  public override string TableName(string schema, string table) => $"[{table}]";

  protected string GetDbFieldTypeString(DbFieldType f) {
    var typestr = 
        f.type == typeof(int) ? "int" : 
        f.type == typeof(decimal) ? "decimal" : 
        f.type == typeof(DateTime) ? "datetime" : 
        f.type == typeof(DateOnly) ? "date" : 
        f.type == typeof(Boolean) ? "bit" : 
        f.type == typeof(Guid) ? "uniqueidentifier" : 
        f.type == typeof(string) && f.length == "max" ? "text" :
        f.type == typeof(string) ? "nvarchar" :
        throw new NotSupportedException(f.type.Name);
    if (!String.IsNullOrWhiteSpace(f.length) && typestr != "text") typestr += $"({f.length})";
    var nullstr = f.required ? "not null" : "null";
    return $"[{f.name}] {typestr} {nullstr}".Trim();
  }

}