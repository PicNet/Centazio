﻿using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl;
using Centazio.Core.Promote;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Stage;
using Centazio.Core.Write;

namespace Centazio.Core;

public abstract class ReadFunction(SystemName system, IEntityStager stager, ICtlRepository ctl) : 
    AbstractFunction<ReadOperationConfig>(system, LifecycleStage.Defaults.Read, new ReadOperationRunner(stager), ctl);

public abstract class PromoteFunction(SystemName system, IStagedEntityRepository stage, ICoreStorage core, ICtlRepository ctl) : 
    AbstractFunction<PromoteOperationConfig>(system, LifecycleStage.Defaults.Promote, new PromoteOperationRunner(stage, core, ctl), ctl);

public abstract class WriteFunction(SystemName system, ICoreStorage core, ICtlRepository ctl) : 
    AbstractFunction<WriteOperationConfig>(system, LifecycleStage.Defaults.Write, new WriteOperationRunner<WriteOperationConfig>(ctl, core), ctl);
