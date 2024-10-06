using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Write;

namespace Centazio.Core;

public static class Containers {
  public record StagedSysCore(StagedEntity Staged, ISystemEntity Sys, ICoreEntity Core);
  public record StagedSys(StagedEntity Staged, ISystemEntity Sys);
  public record StagedCore(StagedEntity Staged, ICoreEntity Core);
  public record StagedIgnore(StagedEntity Staged, ValidString Ignore);
  public record CoreChecksum(ICoreEntity Core, CoreEntityChecksum Checksum);
  
  public static List<StagedCore> ToStagedCore(this List<StagedSysCore> lst) => lst.Select(e => new StagedCore(e.Staged, e.Core)).ToList();
  public static List<StagedSys> ToStagedSys(this List<StagedSysCore> lst) => lst.Select(e => new StagedSys(e.Staged, e.Sys)).ToList();
  public static List<StagedEntity> ToStaged(this List<StagedSysCore> lst) => lst.Select(e => e.Staged).ToList();
  public static List<ISystemEntity> ToSys(this List<StagedSysCore> lst) => lst.Select(e => e.Sys).ToList();
  public static List<ICoreEntity> ToCore(this List<StagedSysCore> lst) => lst.Select(e => e.Core).ToList();
  public static List<ISystemEntity> ToSys(this List<StagedSys> lst) => lst.Select(e => e.Sys).ToList();
  public static List<ICoreEntity> ToCore(this List<StagedCore> lst) => lst.Select(e => e.Core).ToList();
}