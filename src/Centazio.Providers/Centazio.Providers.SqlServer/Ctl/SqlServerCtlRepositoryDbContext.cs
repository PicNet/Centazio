﻿using Centazio.Core.Ctl.Entities;
using Centazio.Providers.EF;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.SqlServer.Ctl;

public class SqlServerCtlRepositoryDbContext(string connstr, string schemanm, string systemstatenm, string objectstatenm, string coretosysmapnm, string entitychangenm) : 
    AbstractCtlRepositoryDbContext(schemanm, systemstatenm, objectstatenm, coretosysmapnm, entitychangenm) {
  
  // ctor with default schema and table names for testing convenience
  internal SqlServerCtlRepositoryDbContext(string connstr) : this(connstr, nameof(Core.Ctl).ToLower(), nameof(SystemState).ToLower(), nameof(ObjectState).ToLower(), nameof(Map.CoreToSysMap).ToLower(), nameof(EntityChange).ToLower()) {}
  
  protected override void ConfigureDbSpecificOptions(DbContextOptionsBuilder options) => options.UseSqlServer(connstr);
}