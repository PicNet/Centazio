﻿namespace Centazio.Core.Misc;

public class Env {

  // todo: add aws or better way to detect this
  public static bool IsHostedEnv() => Environment.GetEnvironmentVariable("CENTAZIO_HOST") == "true" 
      || !String.IsNullOrEmpty(Environment.GetEnvironmentVariable("FUNCTIONS_WORKER_RUNTIME"));
  
  public static bool IsGitHubActions() => Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true";
  
  public static bool IsUnitTest() => Environment.GetEnvironmentVariable("IS_UNIT_TEST") == "true";
  
  public static bool IsCli() => Environment.GetEnvironmentVariable("IS_CLI") == "true";

  public static bool IsInDev() {
    try { FsUtils.GetDevPath(); return true; }
    catch { return false; }
  }

}