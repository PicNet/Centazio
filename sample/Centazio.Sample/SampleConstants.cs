using Centazio.Core.Misc;
using Centazio.Core.Types;

namespace Centazio.Sample;

[IgnoreNamingConventions] public static class SampleConstants { 
  public static class Systems {
    public static readonly SystemName ClickUp = new (nameof(ClickUp));
    public static readonly SystemName GoogleSheets = new (nameof(GoogleSheets));
  }
  
  public static class SystemEntities {
    public static class ClickUp {
      public static readonly SystemEntityTypeName Task = new(nameof(ClickUpTask));
    }
    
    public static class GoogleSheets {
      public static readonly SystemEntityTypeName TaskRow = new(nameof(TaskRow));
    }
  }
  
  public static class CoreEntities {
    public static readonly CoreEntityTypeName Task = new(nameof(CoreTask));
  }
}