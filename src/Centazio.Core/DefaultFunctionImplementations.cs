using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Core.Promote;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Stage;
using Centazio.Core.Types;
using Centazio.Core.Write;

namespace Centazio.Core;

public abstract class ReadFunction(SystemName system, IEntityStager stager, ICtlRepository ctl) : 
    AbstractFunction<ReadOperationConfig>(system, LifecycleStage.Defaults.Read, new ReadOperationRunner(stager), ctl) {
  
  protected ReadOperationResult CreateResult(List<string> results, DateTime? nextcheckpointutc = null) => !results.Any() ? 
      ReadOperationResult.EmptyResult() : 
      ReadOperationResult.Create(results, nextcheckpointutc ?? FunctionStartTime);

}

public abstract class PromoteFunction(SystemName system, IStagedEntityRepository stage, ICoreStorage core, ICtlRepository ctl) : 
    AbstractFunction<PromoteOperationConfig>(system, LifecycleStage.Defaults.Promote, new PromoteOperationRunner(stage, core, ctl), ctl);

public delegate ISystemEntity ConvertCoreToSystemEntityForWritingHandler<E>(SystemEntityId systemid, E coreent) where E : ICoreEntity;

public abstract class WriteFunction(SystemName system, ICoreStorage core, ICtlRepository ctl) : 
    AbstractFunction<WriteOperationConfig>(system, LifecycleStage.Defaults.Write, new WriteOperationRunner<WriteOperationConfig>(ctl, core), ctl) {
  
  protected CovertCoresToSystemsResult CovertCoresToSystems<E>(List<CoreAndPendingCreateMap> tocreate, List<CoreAndPendingUpdateMap> toupdate, ConvertCoreToSystemEntityForWritingHandler<E> converter) where E : ICoreEntity {
    var tocreate2 = tocreate.Select(m => {
      var core = m.CoreEntity.To<E>();
      var sysent = converter(SystemEntityId.DEFAULT_VALUE, core);
      return m.AddSystemEntity(sysent, Config.ChecksumAlgorithm);
    }).ToList();
    var toupdate2 = toupdate.Select(m => {
      var core = m.CoreEntity.To<E>();
      var sysent = converter(m.Map.SystemId, core);
      if (m.Map.SystemEntityChecksum == Config.ChecksumAlgorithm.Checksum(sysent)) throw new Exception($"No changes found on [{typeof(E).Name}] -> [{sysent.GetType().Name}]:" + 
        $"\n\tUpdated Core Entity:[{Json.Serialize(core)}]" +
        $"\n\tUpdated Sys Entity[{sysent}]" +
        $"\n\tExisting Checksum:[{m.Map.SystemEntityChecksum}]" +
        $"\n\tChecksum Subset[{sysent.GetChecksumSubset()}]" +
        $"\n\tChecksum[{Config.ChecksumAlgorithm.Checksum(sysent)}]");
      
      return m.AddSystemEntity(sysent, Config.ChecksumAlgorithm);
    }).ToList();
    return new(tocreate2, toupdate2); 
  }
}
