﻿using Centazio.Core.Misc;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests.Infra;

public class FunctionProjectTests {

  [Test] public void Test_propery_values_are_as_expected() {
    var settings = TestingFactories.Settings();
    var proj = new AzureFunctionProjectMeta(GetType().Assembly, settings, new Templater(settings, TestingFactories.Secrets()));
    
    Assert.That(proj.ProjectName, Is.EqualTo("Centazio.Cli.Tests.Azure"));
    Assert.That(proj.SolutionDirPath, Is.EqualTo(GenRel("Centazio.Cli.Tests.Azure")));
    Assert.That(proj.ProjectDirPath, Is.EqualTo(GenRel("Centazio.Cli.Tests.Azure")));
    Assert.That(proj.CsprojFile, Is.EqualTo("Centazio.Cli.Tests.Azure.csproj"));
    Assert.That(proj.SlnFilePath, Is.EqualTo(GenRel("Centazio.Cli.Tests.Azure", "Centazio.Cli.Tests.Azure.sln")));
    Assert.That(proj.PublishPath, Is.EqualTo(GenRel("Centazio.Cli.Tests.Azure", "bin", "Release", "net9.0", "publish")));
    

    string GenRel(params List<string> steps) => FsUtils.GetSolutionFilePath(steps.Prepend(settings.Defaults.GeneratedCodeFolder).ToList()); 
  }

}