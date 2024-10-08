using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;

namespace Centazio.Core;

public static class Containers {
  public record StagedSysCore(StagedEntity Staged, ISystemEntity Sys, ICoreEntity Core);
  public record StagedSys(StagedEntity Staged, ISystemEntity Sys);
  public record StagedCore(StagedEntity Staged, ICoreEntity Core);
  public record StagedIgnore(StagedEntity Staged, ValidString Ignore);
  public record CoreChecksum(ICoreEntity Core, CoreEntityChecksum CoreEntityChecksum);
  
  public static List<StagedCore> ToStagedCore(this List<StagedSysCore> lst) => lst.Select(e => new StagedCore(e.Staged, e.Core)).ToList();
  public static List<StagedSys> ToStagedSys(this List<StagedSysCore> lst) => lst.Select(e => new StagedSys(e.Staged, e.Sys)).ToList();
  public static List<StagedEntity> ToStaged(this List<StagedSysCore> lst) => lst.Select(e => e.Staged).ToList();
  public static List<ISystemEntity> ToSys(this List<StagedSysCore> lst) => lst.Select(e => e.Sys).ToList();
  public static List<ICoreEntity> ToCore(this List<StagedSysCore> lst) => lst.Select(e => e.Core).ToList();
  public static List<ISystemEntity> ToSys(this List<StagedSys> lst) => lst.Select(e => e.Sys).ToList();
  public static List<E> ToSys<E>(this List<StagedSys> lst) where E : ISystemEntity => lst.Select(e => (E) e.Sys).ToList();
  public static List<E> ToCore<E>(this List<StagedCore> lst) where E : ICoreEntity => lst.Select(e => (E) e.Core).ToList();
  public static List<ICoreEntity> ToCore(this List<StagedCore> lst) => lst.Select(e => e.Core).ToList();
}