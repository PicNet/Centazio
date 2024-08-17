using Centazio.Core;
using Spectre.Console;

namespace Centazio.Cli.Infra;

public static class SpectreCliExtensions {

  public static Table AddRows(this Table tbl, IEnumerable<string[]> rows) {
    rows.ForEachIdx(row => tbl.AddRow(row));
    return tbl;
  }
  

}