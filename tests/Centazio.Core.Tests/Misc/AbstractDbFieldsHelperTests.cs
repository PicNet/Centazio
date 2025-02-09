using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Core.Stage;

namespace Centazio.Core.Tests.Misc;

public class AbstractDbFieldsHelperTests {
  
  private readonly TestDbFieldsHelper dbf = new();
  [Test] public void Test_GetDbFields() {
    var expected = new List<DbFieldType> {
      new(nameof(StagedEntity.Id), typeof(Guid), String.Empty, true),
      new(nameof(StagedEntity.System), typeof(string), "32", true),
      new(nameof(StagedEntity.SystemEntityTypeName), typeof(string), "32", true),
      new(nameof(StagedEntity.DateStaged), typeof(DateTime), String.Empty, true),
      new(nameof(StagedEntity.Data), typeof(string), "max", true),
      new(nameof(StagedEntity.StagedEntityChecksum), typeof(string), "64", true),
      new(nameof(StagedEntity.IgnoreReason), typeof(string), "1024", false),
      new(nameof(StagedEntity.DatePromoted), typeof(DateTime), String.Empty, false)

    };
    Assert.That(dbf.GetDbFields<StagedEntity>(), Is.EquivalentTo(expected));
    Assert.That(dbf.GetDbFields(typeof(StagedEntity)), Is.EquivalentTo(expected));
  }

  class TestDbFieldsHelper : AbstractDbFieldsHelper {

    public override string GenerateCreateTableScript(string schema, string table, List<DbFieldType> fields, string[] pkfields, string? additional = null) => throw new Exception();
    public override string GenerateIndexScript(string schema, string table, params string[] columns) => throw new Exception();
    public override string GenerateDropTableScript(string schema, string table) => throw new Exception();
    public override string TableName(string schema, string table) => throw new Exception();

  }
}
