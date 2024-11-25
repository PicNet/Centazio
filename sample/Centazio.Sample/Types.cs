using Centazio.Core.Misc;

namespace Centazio.Sample;

[IgnoreNamingConventions] 
public record ClickUpTask(string id, string name, ClickUpTask.Status status, string markdown_description, long date_created, long date_updated, long? date_closed, long? date_done, ClickUpTask.Creator creator) {

  public record Status(string status, string type);
  public record Creator(int id, string username);
}