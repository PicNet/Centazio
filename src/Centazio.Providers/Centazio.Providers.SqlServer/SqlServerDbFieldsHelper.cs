using System.Text;
using Centazio.Core.Misc;

namespace Centazio.Providers.SqlServer;

public class SqlServerDbFieldsHelper : AbstractDbFieldsHelper {
  
  public override string GenerateCreateTableScript(string schema, string table, List<DbFieldType> fields, string[] pkfields, List<string[]>? uniques = null, List<ForeignKey>? fks = null) {
    var sql = new StringBuilder();
    if (!String.IsNullOrWhiteSpace(schema)) {
      sql.AppendLine($@"
if not exists (select * from sys.schemas where name = N'{schema}')
  exec('create schema [{schema}] authorization [dbo]');");
    }
    sql.AppendLine($@"
if not exists (select * from sysobjects where name='{table}' and xtype='U')
begin
  create table {TableName(schema, table)} (
    {String.Join(",\n    ", fields.Select(GetDbFieldTypeString))},
    primary key ({String.Join(", ", pkfields)}){GetUniquesSql()}{GetFksSql()}
  )
end
");
    string GetUniquesSql() {
      if (uniques is null || !uniques.Any()) return String.Empty;
      return ",\n" + String.Join(',', uniques.Select(u => $"unique({String.Join(',', u.Select(ColumnName))})"));
    }
    
    string GetFksSql() {
      if (fks is null || !fks.Any()) return String.Empty;
      return ",\n" + String.Join(',', fks.Select(fk => $"foreign key({ String.Join(',', fk.Columns.Select(ColumnName))  }) references {TableName(fk.PkTableSchema, fk.PkTable)}({ String.Join(',', fk.PkColumns.Select(ColumnName)) })"));
    }
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
    return $"{ColumnName(f.Name)} {typestr} {nullstr}";
  }

  public override string GenerateDropTableScript(string schema, string table) =>  $"drop table if exists {TableName(schema, table)}";
  public override string ColumnName(string column) => $"[{column}]";
  public override string TableName(string schema, string table) => $"[{schema}].[{table}]";

  public override string GenerateIndexScript(string schema, string table, params List<string> columns) {
    var name = $"ix_{table}_{String.Join("_", columns.Select(c => c.ToLower()))}";
    return $"drop index if exists {ColumnName(name)} on {TableName(schema, table)};\ncreate index {name} on {TableName(schema, table)} ({String.Join(", ", columns.Select(ColumnName))});";
  }

}