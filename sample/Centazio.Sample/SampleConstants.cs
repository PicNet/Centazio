using Centazio.Core;
using Centazio.Core.Misc;

namespace Centazio.Sample;

[IgnoreNamingConventions] public static class SampleConstants { 
  public static class Systems {
    public static readonly SystemName ClickUp = new (nameof(ClickUp));
    public static readonly SystemName AppSheet = new (nameof(AppSheet));
  }
  
  public static class SystemEntities {
    public static class ClickUp {
      public static readonly SystemEntityTypeName Task = new(nameof(ClickUpTask));
    }
    
    public static class AppSheet {
      public static readonly SystemEntityTypeName Task = new(nameof(AppSheetTask));
    }
  }
  
  public static class CoreEntities {
    public static readonly CoreEntityTypeName Task = new(nameof(CoreTask));
  }
  
  public static class Misc {
    public static readonly string CLICK_UP_OPEN_STATUS = "to do";
    public static readonly string CLICK_UP_COMPLETE_STATUS = "complete";
  }
}