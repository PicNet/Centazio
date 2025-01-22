using Centazio.Core.Misc;
using Centazio.Core.Types;

namespace Centazio.Sample;

[IgnoreNamingConventions] public static class Constants { 
  public static class Systems {
    public static readonly SystemName ClickUp = new ("ClickUp");
  }
  
  public static class SystemEntities {
    public static class ClickUp {
      public static readonly SystemEntityTypeName Task = new(nameof(ClickUpTask));
    }
  }
  
  public static class CoreEntities {
    public static readonly CoreEntityTypeName Task = new(nameof(CoreTask));
  }
}