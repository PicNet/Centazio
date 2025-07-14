using System.Runtime.InteropServices;
using Centazio.Core.Secrets;
using DotNet.Testcontainers.Builders;
using Testcontainers.MsSql;

namespace Centazio.Providers.SqlServer.Tests;

public class SqlConn {
  
  private static SqlConn? instance;
  
  public static async Task<SqlConn> GetInstance(bool real, CentazioSecrets secrets) {
    if (instance is not null) return instance.Real == real ? instance : throw new Exception($"cannot change Real/Container between tests");
    instance = new SqlConn(real, secrets);
    return await instance.Init();
  }
  
  public bool Real { get; }
  
  private readonly CentazioSecrets secrets;
  private MsSqlContainer? container;
  private string? connstr;
  
  public string ConnStr => connstr ?? throw new Exception("not initialised");

  private SqlConn(bool real, CentazioSecrets secrets) {
    if (instance is not null) throw new NotSupportedException();
    (this.secrets, Real, instance) = (secrets, real, this);
  }
  
  private async Task<SqlConn> Init() {
    if (connstr is not null) throw new Exception("already initialised");
    
    connstr = Real ? secrets.SQL_CONN_STR : await ContainerInit();
    if (connstr is null) throw new Exception("connstr was not initialised");
    return this;
    
    async Task<string> ContainerInit() {
      var iswin = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
      var builder = iswin ? new MsSqlBuilder() : new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("/opt/mssql-tools18/bin/sqlcmd", "-C", "-Q", "select 1;"));
      
      container = builder.Build();
      await container.StartAsync();
      return container.GetConnectionString();
    }
  }

  // note: This should only be called by TestSuiteInitialiser.cs
  internal async Task Dispose() {
    if (Real) return;
    if (container is null) throw new Exception("not initialised");
    
    await container.StopAsync();
    await container.DisposeAsync();
  }
}