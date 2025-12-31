using Traceable.Core.Models;

namespace Traceable.Core.Tracerr;

public interface ITracer
{        
    void Log(TraceLevel level, string message, Exception? exception = null);
    
    void Trace(string message);
    void Debug(string message);
    void Info(string message);
    void Warning(string message);
    
    void Error(string message, Exception? exception = null);
    void Fatal(string message, Exception? exception = null);
    
    ITracer WithContext(string key, object value);
    ITracer WithCorrelationId(string correlationId);
}