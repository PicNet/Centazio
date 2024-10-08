using System.Globalization;
using System.Text.RegularExpressions;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;
using Serilog.Parsing;

namespace Centazio.Core;

public interface ILoggable {
  object LoggableValue { get; }
}

public static class LogInitialiser {

  private static readonly string DATE_TIME_FORMAT = "yyyy-MM-ddTHH:mm:ssZ";
  // public static readonly ITextFormatter Formatter = new CustomCompactJsonFormatter();
  public static LoggingLevelSwitch LevelSwitch { get; } = new(LogEventLevel.Debug); 

  public static LoggerConfiguration GetBaseConfig(LogEventLevel level = LogEventLevel.Debug, IList<string>? filters = null) {
    LevelSwitch.MinimumLevel = level;
    var conf = new LoggerConfiguration()
        .Destructure.ByTransformingWhere<ValidString>(typeof(ValidString).IsAssignableFrom, obj => obj.Value)
        .Destructure.ByTransformingWhere<ILoggable>(typeof(ILoggable).IsAssignableFrom, obj => obj.LoggableValue)
        .MinimumLevel.ControlledBy(LevelSwitch);
    if (filters is not null && filters.Any()) { conf = conf.Filter.ByIncludingOnly(log => Regex.Match(log.MessageTemplate.Text, $"({String.Join('|', filters)})").Success); }
    return conf;
  }

  public static LoggerConfiguration GetConsoleConfig(LogEventLevel level = LogEventLevel.Debug, IList<string>? filters = null) => GetBaseConfig(level, filters)
      .WriteTo
      // .Console(Formatter);
      .Console();

  private class CustomJsonValueFormatter() : JsonValueFormatter(null) {

    protected override bool VisitStructureValue(TextWriter state, StructureValue structure) {
      state.Write('{');

      var notnulls = structure.Properties
          .Where(prop => prop.Value is not ScalarValue sv || sv.Value is not null)
          .ToList();
      
      char? delim = null;

      for (var i = 0; i < notnulls.Count; i++) {
        if (delim is not null) state.Write(delim.Value);
        delim = ',';
        var prop = structure.Properties[i];
        WriteQuotedJsonString(prop.Name, state);
        state.Write(':');
        Visit(state, prop.Value);
      }
      state.Write('}');
      return false;
    }
    
    protected override void FormatLiteralValue(object? value, TextWriter output) {
      if (value is DateTime dt) {
        output.Write('\"');
        output.Write(dt.ToString(DATE_TIME_FORMAT));
        output.Write('\"');
        return;
      }
      base.FormatLiteralValue(value, output);
    }

  }

  // copied from Serilog.Formatting.Compact Nuget
  public class CustomCompactJsonFormatter : ITextFormatter {

    private readonly JsonValueFormatter _valueFormatter = new CustomJsonValueFormatter();
    public void Format(LogEvent logEvent, TextWriter output) {
      FormatEvent(logEvent, output, _valueFormatter);
      output.WriteLine();
    }

    public static void FormatEvent(LogEvent logEvent, TextWriter output, JsonValueFormatter valueFormatter) {
      ArgumentNullException.ThrowIfNull(logEvent);
      ArgumentNullException.ThrowIfNull(output);
      ArgumentNullException.ThrowIfNull(valueFormatter);

      output.Write("{\"@t\":\"");
      output.Write(logEvent.Timestamp.UtcDateTime.ToString(DATE_TIME_FORMAT));
      output.Write("\",\"@mt\":");
      JsonValueFormatter.WriteQuotedJsonString(logEvent.MessageTemplate.Text, output);
      var source = logEvent.MessageTemplate.Tokens.OfType<PropertyToken>().Where((Func<PropertyToken, bool>)(pt => pt.Format is not null)).ToList();
      if (source.Any()) {
        output.Write(",\"@r\":[");
        var str = String.Empty;
        foreach (var propertyToken in source) {
          output.Write(str);
          str = ",";
          var stringWriter = new StringWriter();
          var properties = logEvent.Properties;
          var output1 = stringWriter;
          var invariantCulture = CultureInfo.InvariantCulture;
          propertyToken.Render(properties, output1, invariantCulture);
          JsonValueFormatter.WriteQuotedJsonString(stringWriter.ToString(), output);
        }

        output.Write(']');
      }

      if (logEvent.Level != LogEventLevel.Information) {
        output.Write(",\"@l\":\"");
        output.Write(logEvent.Level);
        output.Write('"');
      }

      if (logEvent.Exception is not null) {
        output.Write(",\"@x\":");
        JsonValueFormatter.WriteQuotedJsonString(logEvent.Exception.ToString(), output);
      }

      if (logEvent.TraceId.HasValue) {
        output.Write(",\"@tr\":\"");
        output.Write(logEvent.TraceId.Value.ToHexString());
        output.Write('"');
      }

      if (logEvent.SpanId.HasValue) {
        output.Write(",\"@sp\":\"");
        output.Write(logEvent.SpanId.Value.ToHexString());
        output.Write('"');
      }

      foreach (var property in logEvent.Properties) {
        var str = property.Key;
        if (str.Length > 0 && str[0] == '@')
          str = "@" + str;
        output.Write(',');
        JsonValueFormatter.WriteQuotedJsonString(str, output);
        output.Write(':');
        valueFormatter.Format(property.Value, output);
      }

      output.Write('}');
    }

  }

}