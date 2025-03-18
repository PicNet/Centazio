using Centazio.Core.Misc;

namespace Centazio.Providers.Sqlite;

public class SqliteDbFieldsHelper : AbstractDbFieldsHelper {

  public override string GenerateCreateTableScript(string schema, string table, List<DbFieldType> fields, string[] pkfields, string? additional=null) {
    var additionaltxt = String.IsNullOrWhiteSpace(additional) ? String.Empty : ",\n  " + additional;
    return $@"CREATE TABLE IF NOT EXISTS {TableName(schema, table)} (
  {String.Join(",\n    ", fields.Select(GetDbFieldTypeString))},
  PRIMARY KEY ({String.Join(", ", pkfields)}){additionaltxt})";
  }

  public override string GenerateIndexScript(string schema, string table, params List<string> columns) => 
      $"CREATE INDEX IF NOT EXISTS ix_{table}_{String.Join("_", columns.Select(c => c.ToLower()))} ON {TableName(schema, table)} ({String.Join(", ", columns)});";

  public override string GenerateDropTableScript(string schema, string table) =>  $"DROP TABLE IF EXISTS {TableName(schema, table)}";
  public override string TableName(string schema, string table) => $"[{table}]";

  protected string GetDbFieldTypeString(DbFieldType f) {
    var typestr = 
        f.FieldType == typeof(int) ? "int" : 
        f.FieldType == typeof(decimal) ? "decimal" : 
        f.FieldType == typeof(DateTime) ? "datetime" : 
        f.FieldType == typeof(DateOnly) ? "date" : 
        f.FieldType == typeof(Boolean) ? "bit" : 
        f.FieldType == typeof(Guid) ? "uniqueidentifier" : 
        f.FieldType == typeof(string) && f.Length == "max" ? "text" :
        f.FieldType == typeof(string) ? "nvarchar" :
        throw new NotSupportedException(f.FieldType.Name);
    if (!String.IsNullOrWhiteSpace(f.Length) && typestr != "text") typestr += $"({f.Length})";
    var nullstr = f.Required ? "not null" : "null";
    return $"[{f.Name}] {typestr} {nullstr}".Trim();
  }

}