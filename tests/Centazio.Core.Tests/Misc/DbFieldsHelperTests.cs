using System.Text.RegularExpressions;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.Misc;

public class DbFieldsHelperTests {

  [Test] public void Test_GetObjectDbFields() {
    var list = new DbFieldsHelper().GetDbFields(typeof(CoreEntity));
    Assert.That(list, Has.Member(new DbFieldType(nameof(CoreEntity.FirstName), "nvarchar", "64", true)));
    Assert.That(list, Has.Member(new DbFieldType(nameof(CoreEntity.DateOfBirth), "date", String.Empty, true)));
    Assert.That(list.FindIndex(t => t.name == nameof(CoreEntity.DisplayName)), Is.EqualTo(-1)); // interface has [JsonIgnore]
  }
  
  [Test] public void Test_GetSqlServerCreateTableScript() {
    var dbf = new DbFieldsHelper();
    var sql = dbf.GetSqlServerCreateTableScript("schemaname", nameof(SystemState), dbf.GetDbFields<SystemState>(), [nameof(SystemState.System), nameof(SystemState.Stage)]);
    var exp = @"IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'schemaname')
  EXEC('CREATE SCHEMA [schemaname] AUTHORIZATION [dbo]');

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SystemState' AND xtype='U')
BEGIN
  CREATE TABLE [schemaname].[SystemState] (
    [System] nvarchar(32) not null,
    [Stage] nvarchar(32) not null,
    [DateCreated] datetime2 not null,
    [Active] bit not null,
    [Status] nvarchar(128) not null,
    [DateUpdated] datetime2 null,
    [LastStarted] datetime2 null,
    [LastCompleted] datetime2 null,
    PRIMARY KEY (System, Stage)
  )
END";
    Assert.That(WS(sql), Is.EqualTo(WS(exp)));
  }
  
  [Test] public void Test_GetSqliteCreateTableScript() {
    var dbf = new DbFieldsHelper();
    var sql = dbf.GetSqliteCreateTableScript(nameof(SystemState), dbf.GetDbFields<SystemState>(), [nameof(SystemState.System), nameof(SystemState.Stage)]);
    var exp = $@"CREATE TABLE IF NOT EXISTS [SystemState] (
  [System] nvarchar(32) not null,
  [Stage] nvarchar(32) not null,
  [DateCreated] datetime not null,
  [Active] bit not null,
  [Status] nvarchar(128) not null,
  [DateUpdated] datetime null,
  [LastStarted] datetime null,
  [LastCompleted] datetime null,
  PRIMARY KEY (System, Stage)
)
";
    Assert.That(WS(sql), Is.EqualTo(WS(exp)));
  }
  
  private string WS(string sql) => Regex.Replace(sql, @"\s+", String.Empty);
}