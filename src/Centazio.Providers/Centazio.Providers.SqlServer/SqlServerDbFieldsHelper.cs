﻿using System.Text;
using Centazio.Core.Misc;

namespace Centazio.Providers.SqlServer;

// todo: stop using capitals for all sql helpers
public class SqlServerDbFieldsHelper : AbstractDbFieldsHelper {
  
  public override string GenerateCreateTableScript(string schema, string table, List<DbFieldType> fields, string[] pkfields, List<string[]>? uniques = null, List<ForeignKey>? fks = null) {
    var sql = new StringBuilder();
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
    PRIMARY KEY ({String.Join(", ", pkfields)}){GetUniquesSql()}{GetFksSql()}
  )
END
");
    string GetUniquesSql() {
      if (uniques is null || !uniques.Any()) return String.Empty;
      return ",\n" + String.Join(',', uniques.Select(u => $"UNIQUE({String.Join(',', u.Select(ColumnName))})"));
    }
    
    string GetFksSql() {
      if (fks is null || !fks.Any()) return String.Empty;
      return ",\n" + String.Join(',', fks.Select(fk => $"FOREIGN KEY({ String.Join(',', fk.Columns.Select(ColumnName))  }) REFERENCES {TableName(fk.PkTableSchema, fk.PkTable)}({ String.Join(',', fk.PkColumns.Select(ColumnName)) })"));
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

  public override string GenerateDropTableScript(string schema, string table) =>  $"DROP TABLE IF EXISTS {TableName(schema, table)}";
  public override string ColumnName(string column) => $"[{column}]";
  public override string TableName(string schema, string table) => $"[{schema}].[{table}]";

  public override string GenerateIndexScript(string schema, string table, params List<string> columns) {
    var name = $"ix_{table}_{String.Join("_", columns.Select(c => c.ToLower()))}";
    return $"DROP INDEX IF EXISTS {ColumnName(name)} ON {TableName(schema, table)};\nCREATE INDEX {name} ON {TableName(schema, table)} ({String.Join(", ", columns.Select(ColumnName))});";
  }

}