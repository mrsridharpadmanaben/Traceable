using System.Threading.Channels;
using Traceable.Core.Models;
using Traceable.Core.Sinks;

namespace Traceable.Core;

public class TraceProcessor : IDisposable
{
    private readonly Channel<(TraceEntry, List<ITraceSink>)> _channel;
    private readonly Task _processingTask;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public TraceProcessor(int capacity = 10000)
    {
        var options = new BoundedChannelOptions(capacity) { FullMode = BoundedChannelFullMode.DropOldest, };
        _channel = Channel.CreateBounded<(TraceEntry, List<ITraceSink>)>(options);
        _cancellationTokenSource = new CancellationTokenSource();
        _processingTask = Task.Run(ProcessTraces);
    }

    public bool Enqueue(TraceEntry entry, List<ITraceSink> sinks)
    {
        return _channel.Writer.TryWrite((entry, sinks));
    }

    private async Task ProcessTraces()
    {
        await foreach (var (entry, sinks) in _channel.Reader.ReadAllAsync(_cancellationTokenSource.Token))
        {
            var tasks = sinks.Select(async sink =>
            {
                try
                {
                    await sink.WriteAsync(entry);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ProcessTraces] Sink {sink.GetType().Name} failed: {ex.Message}");
                }
            });
            await Task.WhenAll(tasks);
        }
    }

    public async Task FlushAsync()
    {
        _channel.Writer.Complete();
        await _processingTask;
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _channel.Writer.Complete();
        _processingTask.Wait(TimeSpan.FromSeconds(5));
        _cancellationTokenSource.Dispose();
    }
}