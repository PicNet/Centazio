using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Centazio.Core;

public interface ILoggable {
  [JsonIgnore] string LoggableValue { get; }
}

public static class LogInitialiser {
  public static LoggingLevelSwitch LevelSwitch { get; } = new(LogEventLevel.Debug); 

  public static LoggerConfiguration GetBaseConfig(LogEventLevel level = LogEventLevel.Debug, List<string>? filters = null) {
    LevelSwitch.MinimumLevel = level;
    var conf = new LoggerConfiguration()
        .Destructure.ByTransformingWhere<ValidString>(typeof(ValidString).IsAssignableFrom, obj => obj.Value)
        .Destructure.ByTransformingWhere<ILoggable>(typeof(ILoggable).IsAssignableFrom, obj => obj.LoggableValue)
        .MinimumLevel.ControlledBy(LevelSwitch);
    if (filters is not null && filters.Any()) { conf = conf.Filter.ByIncludingOnly(log => Regex.Match(log.MessageTemplate.Text, $"({String.Join('|', filters)})").Success); }
    return conf;
  }

  public static LoggerConfiguration GetConsoleConfig(LogEventLevel level = LogEventLevel.Debug, List<string>? filters = null) => GetBaseConfig(level, filters)
      .WriteTo
      .Console();
}