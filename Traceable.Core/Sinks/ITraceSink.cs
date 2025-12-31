using Traceable.Core.Models;

namespace Traceable.Core.Sinks;

public interface ITraceSink
{
    Task WriteAsync(TraceEntry entry);
    Task FlushAsync();
    
    void Dispose();
}