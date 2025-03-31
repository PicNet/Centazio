﻿namespace Centazio.Core.Misc;

public class Env {

  public static bool IsCloudEnviornment() => !String.IsNullOrEmpty(Environment.GetEnvironmentVariable("FUNCTIONS_WORKER_RUNTIME"));
  
  public static bool IsGitHubActions() => Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true";
  
  public static bool IsUnitTest() => UtcDate.Utc.GetType().Name == "TestingUtcDate";

  public static bool IsCentazioDevDir() {
    if (IsCloudEnviornment() || IsGitHubActions()) return false;
    try { FsUtils.GetSolutionRootDirectory(); return true; }
    catch { return false; }
  }

}