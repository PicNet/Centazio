using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Test.Lib;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.EF.Tests;

public class TestingEfCtlRepository(Func<AbstractCtlRepositoryDbContext> getdb, IDbFieldsHelper dbf) : EFCtlRepository(getdb), ITestingCtlRepository {

  public async Task<List<Map.CoreToSysMap>> GetAllMaps() {
    await using var db = getdb();
    return (await db.CoreToSystemMaps.ToListAsync()).Select(dto => dto.ToBase()).ToList();
  }
  
  public override async Task<ICtlRepository> Initialise() {
    await using var db = getdb();
    await DropTablesImpl(dbf, db);
    await CreateSchema(dbf, db);
    return await base.Initialise();
  }
  
  public override async ValueTask DisposeAsync() {
    await using var db = getdb();
    await DropTablesImpl(dbf, db);
  }
  
}