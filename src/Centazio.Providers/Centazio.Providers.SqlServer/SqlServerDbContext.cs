﻿using Centazio.Providers.EF;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.SqlServer;

public abstract class SqlServerDbContext(string connstr) : CentazioDbContext {

  protected override void ConfigureDbSpecificOptions(DbContextOptionsBuilder options) => options.UseSqlServer(connstr);
  
  protected override string ToTableName(TableName table) => $"[{table.Schema}].[{table.Table}]";
}