using System.Text;
using System.Text.Json;

namespace Traceable.Core.Models;

public class TraceEntry
{
    public DateTime Timestamp { get; set; }
    public TraceLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public ExceptionInfo? Exception { get; set; }
    public Dictionary<string, object> Context { get; set; }
    public string MachineName { get; set; }
    public string ThreadId { get; set; }

    public TraceEntry()
    {
        Timestamp = DateTime.UtcNow;
        Context =  new Dictionary<string, object>();
        MachineName = Environment.MachineName;
        ThreadId = Thread.CurrentThread.ManagedThreadId.ToString();
    }

    public string ToJson()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented =  true
        };

        return JsonSerializer.Serialize(this, options);
    }

    public string ToPlainText()
    {
        StringBuilder log = new StringBuilder();
        
        log.Append($"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}]");
        log.Append($"[{Level}]");
        if (!string.IsNullOrEmpty(CorrelationId)) log.Append($"[{CorrelationId}]");
        log.Append($"[{Message}]");
        
        if (Context.Count != 0)
        {
            log.Append(" | Context: {");
            log.Append(string.Join(", ", Context.Select(kvp => $"{kvp.Key}={kvp.Value}")));
            log.Append('}');
        }

        if (Exception is null) return log.ToString();
        
        log.AppendLine();
        log.Append($"  Exception: {Exception.Type} - {Exception.Message}");
        
        if (string.IsNullOrEmpty(Exception.StackTrace)) return log.ToString();
        
        log.AppendLine();
        log.Append($"  StackTrace: {Exception.StackTrace}");

        return log.ToString();
    }
}