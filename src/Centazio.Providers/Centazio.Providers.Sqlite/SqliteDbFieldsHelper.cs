using Centazio.Core.Misc;

namespace Centazio.Providers.Sqlite;

public class SqliteDbFieldsHelper : AbstractDbFieldsHelper {

  public override string GenerateCreateTableScript(string schema, string table, List<DbFieldType> fields, string[] pkfields, List<string[]>? uniques = null, List<ForeignKey>? fks = null) {
    return $@"CREATE TABLE IF NOT EXISTS {TableName(schema, table)} (
  {String.Join(",\n    ", fields.Select(GetDbFieldTypeString))},
  PRIMARY KEY ({String.Join(", ", pkfields)}){GetUniquesSql()}{GetFksSql()})";
    
    string GetUniquesSql() {
      if (uniques is null || !uniques.Any()) return String.Empty;
      return String.Join(',', uniques.Select(u => $"UNIQUE({String.Join(',', u.Select(ColumnName))})"));
    }
    
    string GetFksSql() {
      if (fks is null || !fks.Any()) return String.Empty;
      return String.Join(',', fks.Select(fk => $"FOREIGN KEY({ String.Join(',', fk.Columns.Select(ColumnName))  }) REFERENCES {TableName(fk.PkTableSchema, fk.PkTable)}({ String.Join(',', fk.PkColumns.Select(ColumnName)) })"));
    }
  }

  public override string GenerateIndexScript(string schema, string table, params List<string> columns) => 
      $"CREATE INDEX IF NOT EXISTS ix_{table}_{String.Join("_", columns.Select(c => c.ToLower()))} ON {TableName(schema, table)} ({String.Join(", ", columns)});";

  public override string GenerateDropTableScript(string schema, string table) =>  $"DROP TABLE IF EXISTS {TableName(schema, table)}";
  public override string ColumnName(string column) => $"[{column}]";
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