using System.Text.RegularExpressions;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Test.Lib;

namespace Centazio.Providers.Sqlite.Tests;

public class SqliteDbFieldsHelperTests {

  [Test] public void Test_GetObjectDbFields() {
    var list = new SqliteDbFieldsHelper().GetDbFields(typeof(CoreEntity));
    Assert.That(list, Has.Member(new DbFieldType(nameof(CoreEntity.FirstName), typeof(string), "64", true)));
    Assert.That(list, Has.Member(new DbFieldType(nameof(CoreEntity.DateOfBirth), typeof(DateOnly), String.Empty, true)));
    Assert.That(list.FindIndex(t => t.name == nameof(CoreEntity.DisplayName)), Is.EqualTo(-1)); // interface has [JsonIgnore]
  }
  
  [Test] public void Test_GenerateCreateTableScript() {
    var dbf = new SqliteDbFieldsHelper();
    var sql = dbf.GenerateCreateTableScript("schemaname", nameof(SystemState), dbf.GetDbFields<SystemState>(), [nameof(SystemState.System), nameof(SystemState.Stage)]);
    var exp = $@"CREATE TABLE IF NOT EXISTS [SystemState] (
  [System] nvarchar(32) not null,
  [Stage] nvarchar(32) not null,
  [DateCreated] datetime not null,
  [DateUpdated] datetime not null,
  [Active] bit not null,
  [Status] nvarchar(128) not null,  
  [LastStarted] datetime null,
  [LastCompleted] datetime null,
  PRIMARY KEY (System, Stage)
)
";
    Assert.That(WS(sql), Is.EqualTo(WS(exp)));
  }
  
  private string WS(string sql) => Regex.Replace(sql, @"\s+", String.Empty);
}