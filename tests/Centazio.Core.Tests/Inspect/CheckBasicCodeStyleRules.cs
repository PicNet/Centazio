namespace Centazio.Core.Tests.Inspect;

public class CheckBasicCodeStyleRules {

  [Test] public void Do_basic_code_style_checks() {
    var braces = @"\)\s*\n\s*\{";
    var tab = @"\t";
    var catchstmt = @"^(?:(?!try).)*}\s*\n\s*catch";
    var indent = @"^INDENT1\w.*\{\s*\n+INDENT2";
    var maxindent = 5;
    var errors = new List<string>();
    InspectUtils.CsFiles(null).ForEach((file, idx) => {
      var content = File.ReadAllText(file);
      if (Regex.IsMatch(content, braces)) errors.Add($"file[{file}] has braces on new lines.  Use K&R style braces.");
      if (Regex.IsMatch(content, tab)) errors.Add($"file[{file}] uses tab characters, replace with 2 space indentations.");
      if (Regex.IsMatch(content, catchstmt, RegexOptions.Multiline)) errors.Add($"file[{file}] has catch statement on new line. Use K&R style braces.");
      _ = Enumerable.Range(0, maxindent - 1).Any(sz => {
        if (!Regex.IsMatch(content, GetIndentationPatternForSize(sz, true), RegexOptions.Multiline)) return false;
        errors.Add($"file[{file}] does not appear to use 2 space indentations [failed sz={sz}]");
        return true;
      });
      if (Regex.IsMatch(content, GetIndentationPatternForSize(maxindent, false), RegexOptions.Multiline)) errors.Add($"file[{file}] uses {maxindent} levels of indentation which is too much");
    });
    Assert.That(errors, Is.Empty, String.Join("\n", errors));

    string GetIndentationPatternForSize(int sz, bool addspace) {
      var pattern = indent.Replace("INDENT1", new String(' ', sz * 2)).Replace("INDENT2", new String(' ', (sz + 1) * 2));
      return pattern + (addspace ? " " : String.Empty);
    }
  }

}