using System.Net;
using System.Net.Sockets;
using NUnit.Framework;

namespace Centazio.Test.Lib;

public static class AzTestingEnv {

  public static async Task IgnoreIfVaultUnreachable() {
    var host = $"{(await F.Settings()).AzureSettings.KeyVaultName}.vault.azure.net";
    try { await Dns.GetHostAddressesAsync(host); }
    catch (SocketException) { Assert.Ignore($"azure key vault unreachable [{host}]"); }
  }

}
