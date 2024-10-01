using Spectre.Console;

namespace Centazio.Cli.Infra;

public static class SpectreCliExtensions {

  public static Table AddRows(this Table tbl, List<string[]> rows) {
    rows.ForEach(row => tbl.AddRow(row));
    return tbl;
  }
  

}