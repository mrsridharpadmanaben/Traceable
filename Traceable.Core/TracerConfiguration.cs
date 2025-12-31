using Traceable.Core.Models;
using Traceable.Core.Sinks;
using Traceable.Core.Sinks.Console;
using Traceable.Core.Sinks.Filee;
using Traceable.Core.Sinks.Remote;
using Traceable.Core.Tracerr;

namespace Traceable.Core;

public class TracerConfiguration
{
    private TraceLevel _minLevel = TraceLevel.Info;
    private readonly List<ITraceSink> _sinks = new();
    private string _serviceName = "DefaultService";
    private int _channelCapacity = 10000;
    
    public TracerConfiguration SetMinimumLevel(TraceLevel level)
    {
        _minLevel = level;
        return this;
    }

    public TracerConfiguration SetServiceName(string name)
    {
        _serviceName = name;
        return this;
    }

    public TracerConfiguration SetChannelCapacity(int capacity)
    {
        _channelCapacity = capacity;
        return this;
    }

    public TracerConfiguration WriteTo(ITraceSink sink)
    {
        _sinks.Add(sink);
        return this;
    }

    public TracerConfiguration WriteToConsole(bool useJson = false)
    {
        _sinks.Add(new ConsoleSink(useJson));
        return this;
    }

    public TracerConfiguration WriteToFile(string filePath, bool useJson = false, long maxFileSizeMegaBytes = 10, int maxArchiveFiles = 5)
    {
        _sinks.Add(new FileSink(filePath, useJson, maxFileSizeMegaBytes, maxArchiveFiles));
        return this;
    }
    
    public TracerConfiguration WriteToRemote(string endpoint, int batchSize = 50, int flushIntervalSeconds = 5)
    {
        _sinks.Add(new RemoteSink(endpoint, batchSize, flushIntervalSeconds));
        return this;
    }

    public Tracer CreateTracer()
    {
        var processor = new TraceProcessor(_channelCapacity);
        return new Tracer(_serviceName, _minLevel, _sinks, processor);
    }
}