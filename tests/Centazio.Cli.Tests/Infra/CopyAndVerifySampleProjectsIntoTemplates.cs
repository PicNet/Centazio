using Centazio.Core.Misc;

namespace Centazio.Cli.Tests.Infra;

public class CopyAndVerifySampleProjectsIntoTemplates {

  [Test] public void Copy_and_verify_shared_solution_cs_files() {
    var (from, to) = (FsUtils.GetSolutionFilePath("sample", "Centazio.Sample.Shared"), FsUtils.GetSolutionFilePath("defaults", "templates", "centazio", "Solution.Shared"));
    FsUtils.CopyDirFiles(from, to, "*.cs", true);
  }

  [Test] public void Copy_and_verify_shared_function_cs_files() {
    var (from, to) = (FsUtils.GetSolutionFilePath("sample", "Centazio.Sample.ClickUp"), FsUtils.GetSolutionFilePath("defaults", "templates", "centazio", "Functions"));
    FsUtils.CopyDirFiles(from, to, "*.cs", true);
  }
  
}