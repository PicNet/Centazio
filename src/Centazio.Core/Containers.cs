using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;

namespace Centazio.Core;

public static class Containers {
  public readonly record struct StagedSysCore(StagedEntity Staged, ISystemEntity Sys, ICoreEntity Core, bool IsCreated);
  
  public record StagedSysOptionalCore(StagedEntity Staged, ISystemEntity Sys, ICoreEntity? OptCore) {
    public StagedSysCore SetCore(ICoreEntity core) => new(Staged, Sys, core, OptCore is null);
  }
  
  public record StagedSys(StagedEntity Staged, ISystemEntity Sys);
  public record StagedIgnore(StagedEntity Staged, ValidString Ignore);
  public record CoreChecksum(ICoreEntity Core, CoreEntityChecksum CoreEntityChecksum);
  
  public static List<ICoreEntity> ToCore(this List<StagedSysCore> lst) => lst.Select(e => e.Core).ToList();
  public static List<CoreEntityId> ToCoreId(this List<StagedSysCore> lst) => lst.Select(e => e.Core.CoreId).ToList();
}