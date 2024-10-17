using Centazio.Core.Ctl.Entities;
using Centazio.Test.Lib;

namespace Centazio.Providers.SqlServer.Tests;

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
    Assert.That(sql.Trim().Replace("\r\n", "\n"), Is.EqualTo(@"IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'schemaname')
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
END".Replace("\r\n", "\n")));
  }
}