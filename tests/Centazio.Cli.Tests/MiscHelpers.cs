using Centazio.Cli.Commands.Gen;
using Centazio.Cli.Infra;
using Centazio.Core.Misc;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests;

public static class MiscHelpers {
  public static FunctionProjectMeta EmptyFunctionProject(ECloudEnv cloud) => 
      new (ReflectionUtils.LoadAssembly("Centazio.TestFunctions"), cloud, TestingFactories.Settings().GeneratedCodeFolder);

}