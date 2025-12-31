using Traceable.Core.Models;
using Traceable.Core.Sinks;

namespace Traceable.Core.Tracerr;

public class Tracer: ITracer, IDisposable
{
    private readonly TraceLevel _minLevel;
    private readonly List<ITraceSink> _sinks;
    private readonly Dictionary<string, object> _context;
    private readonly string? _correlationId;
    private readonly TraceProcessor _processor;

    private readonly string _serviceName;

    public Tracer(string serviceName, TraceLevel minLevel, List<ITraceSink> sinks, TraceProcessor processor)
    {
        _serviceName = serviceName;
        _minLevel = minLevel;
        _sinks = sinks;
        _processor = processor;
        _context = new Dictionary<string, object>();
        _correlationId = null;
    }
    
    private Tracer(Tracer parent, Dictionary<string, object> context, string? correlationId)
    {
        _serviceName = parent._serviceName;
        _minLevel = parent._minLevel;
        _sinks = parent._sinks;
        _processor = parent._processor;
        _context = new Dictionary<string, object>(context);
        _correlationId = correlationId ?? parent._correlationId;
    }
    
    public void Log(TraceLevel level, string message, Exception? exception = null)
    {
        if (level < _minLevel)
            return;

        var entry = new TraceEntry
        {
            Level = level,
            Message = message,
            ServiceName = _serviceName,
            Exception = ExceptionInfo.FromException(exception),
            Context = new Dictionary<string, object>(_context),
            CorrelationId = _correlationId
        };

        _processor.Enqueue(entry, _sinks);
    }

    public void Trace(string message) => Log(TraceLevel.Trace, message);
    public void Debug(string message) => Log(TraceLevel.Debug, message);
    public void Info(string message) => Log(TraceLevel.Info, message);
    public void Warning(string message) => Log(TraceLevel.Warning, message);
    public void Error(string message, Exception? exception = null) => Log(TraceLevel.Error, message, exception);
    public void Fatal(string message, Exception? exception = null) => Log(TraceLevel.Fatal, message, exception);


    public ITracer WithContext(string key, object value)
    {
        var newContext = new Dictionary<string, object>(_context) { [key] = value };
        return new Tracer(this, newContext, _correlationId);
    }

    public ITracer WithCorrelationId(string correlationId)
    {
        return new Tracer(this, _context, correlationId);
    }

    public async Task FlushAsync()
    {
        await _processor.FlushAsync();
        foreach (var sink in _sinks)
        {
            await sink.FlushAsync();
        }
    }

    public void Dispose()
    {
        FlushAsync().Wait();
        _processor?.Dispose();
        foreach (var sink in _sinks)
        {
            sink?.Dispose();
        }
    }
}