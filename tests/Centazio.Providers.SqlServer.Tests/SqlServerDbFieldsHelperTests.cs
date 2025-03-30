using System.Text.RegularExpressions;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Test.Lib;

namespace Centazio.Providers.SqlServer.Tests;

public class SqlServerDbFieldsHelperTests {

  [Test] public void Test_GetObjectDbFields() {
    var list = new SqlServerDbFieldsHelper().GetDbFields(typeof(CoreEntity));
    Assert.That(list, Has.Member(new DbFieldType(nameof(CoreEntity.FirstName), typeof(string), "64", true)));
    Assert.That(list, Has.Member(new DbFieldType(nameof(CoreEntity.DateOfBirth), typeof(DateOnly), String.Empty, true)));
    Assert.That(list.FindIndex(t => t.Name == nameof(CoreEntity.DisplayName)), Is.EqualTo(-1)); // interface has [JsonIgnore]
  }
  
  [Test] public void Test_GetSqlServerCreateTableScript() {
    var dbf = new SqlServerDbFieldsHelper();
    var sql = dbf.GenerateCreateTableScript("schemaname", nameof(SystemState), dbf.GetDbFields<SystemState>(), [nameof(SystemState.System), nameof(SystemState.Stage)]);
    var exp = @"if not exists (select * from sys.schemas where name = N'schemaname')
  exec('create schema [schemaname] authorization [dbo]');

if not exists (select * from sysobjects where name='SystemState' and xtype='U')
begin
  create table [schemaname].[SystemState] (
    [System] nvarchar(32) not null,
    [Stage] nvarchar(32) not null,
    [DateCreated] datetime2 not null,
    [DateUpdated] datetime2 not null,
    [Active] bit not null,
    [Status] nvarchar(128) not null,    
    [LastStarted] datetime2 null,
    [LastCompleted] datetime2 null,
    primary key (System, Stage)
  )
end";
    Assert.That(WS(sql), Is.EqualTo(WS(exp)));
  }
  
  
  private string WS(string sql) => Regex.Replace(sql, @"\s+", String.Empty);
}