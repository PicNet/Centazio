using System.Text.RegularExpressions;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Test.Lib;

namespace Centazio.Providers.PostgresSql.Tests;

public class PostgresSqlDbFieldsHelperTests {

  [Test] public void Test_GetObjectDbFields() {
    var list = new PostgresSqlDbFieldsHelper().GetDbFields(typeof(CoreEntity));
    Assert.That(list, Has.Member(new DbFieldType(nameof(CoreEntity.FirstName), typeof(string), "64", true)));
    Assert.That(list, Has.Member(new DbFieldType(nameof(CoreEntity.DateOfBirth), typeof(DateOnly), String.Empty, true)));
    Assert.That(list.FindIndex(t => t.Name == nameof(CoreEntity.DisplayName)), Is.EqualTo(-1)); // interface has [JsonIgnore]
  }
  
  [Test] public void Test_GenerateCreateTableScript() {
    var dbf = new PostgresSqlDbFieldsHelper();
    var sql = dbf.GenerateCreateTableScript("schemaname", nameof(SystemState), dbf.GetDbFields<SystemState>(), [nameof(SystemState.System), nameof(SystemState.Stage)]);
    var exp = $@"create schema if not exists schemaname;

create table if not exists schemaname.SystemState (
  ""System"" varchar(32) not null,
  ""Stage"" varchar(32) not null,
  ""DateCreated"" timestamp not null,
  ""DateUpdated"" timestamp not null,
  ""Active"" boolean not null,
  ""Status"" varchar(128) not null,  
  ""LastStarted"" timestamp null,
  ""LastCompleted"" timestamp null,
  primary key (""System"", ""Stage"")
);
";
    Assert.That(WS(sql), Is.EqualTo(WS(exp)));
  }
  
  private string WS(string sql) => Regex.Replace(sql, @"\s+", String.Empty);
}