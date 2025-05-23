﻿using System.Text;
using Centazio.Core.Misc;

namespace Centazio.Providers.PostgresSql;

public class PostgresSqlDbFieldsHelper : AbstractDbFieldsHelper {
  
  public override string GenerateCreateTableScript(string schema, string table, List<DbFieldType> fields, string[] pkfields, List<string[]>? uniques = null, List<ForeignKey>? fks = null) {
    var sql = new StringBuilder();
    if (!String.IsNullOrWhiteSpace(schema)) { sql.AppendLine($"create schema if not exists {schema};"); }
    
    sql.AppendLine($@"
create table if not exists {TableName(schema, table)} (
  {String.Join(",\n    ", fields.Select(GetDbFieldTypeString))},
  primary key ({String.Join(", ", pkfields.Select(ColumnName))}){GetUniquesSql()}{GetFksSql()}
);");
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
        f.FieldType == typeof(DateTime) ? "timestamp" : 
        f.FieldType == typeof(DateOnly) ? "date" : 
        f.FieldType == typeof(Boolean) ? "boolean" : 
        f.FieldType == typeof(Guid) ? "uuid" :
        f.FieldType == typeof(string) && f.Length == "max" ? "text" :
        f.FieldType == typeof(string) ? "varchar" : 
        throw new NotSupportedException(f.FieldType.Name);
    if (!String.IsNullOrWhiteSpace(f.Length) && f.Length != "max") typestr += $"({f.Length})";
    var nullstr = f.Required ? "not null" : "null";
    return $"\"{f.Name}\" {typestr} {nullstr}";
  }

  public override string GenerateDropTableScript(string schema, string table) =>  $"drop table if exists {TableName(schema, table)}";
  public override string ColumnName(string column) => $"\"{column}\"";
  public override string TableName(string schema, string table) => $"{schema}.{table}";

  public override string GenerateIndexScript(string schema, string table, params List<string> columns) {
    var name = $"ix_{table}_{String.Join("_", columns.Select(c => c.ToLower()))}";
    return $"create index if not exists {name} on {TableName(schema, table)} ({String.Join(", ", columns.Select(ColumnName))});";
  }

}