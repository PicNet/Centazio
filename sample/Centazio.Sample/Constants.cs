using Centazio.Core;
using Centazio.Core.Misc;
using Centazio.Core.Types;

namespace Centazio.Sample;

[IgnoreNamingConventions]
public static class Constants { 
  public static readonly SystemName CLICK_UP = new ("ClickUp");
  public static readonly SystemEntityTypeName CU_TASK = new(nameof(ClickUpTask));
  

}