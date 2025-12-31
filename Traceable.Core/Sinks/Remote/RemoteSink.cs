using System.Text;
using System.Text.Json;
using Traceable.Core.Models;

namespace Traceable.Core.Sinks.Remote;

public class RemoteSink : ITraceSink
{
    private readonly string _endpoint;
    private readonly HttpClient _httpClient;
    private readonly int _batchSize;
    
    private readonly List<TraceEntry> _entries = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    
    private readonly Timer _flushTimer;
    private readonly CircuitBreaker _circuitBreaker;

    public RemoteSink(string endpoint, int batchSize = 50, int flushIntervalSeconds = 5)
    {
        _endpoint = endpoint;
        _batchSize = batchSize;
        
        _httpClient = new HttpClient{Timeout = TimeSpan.FromSeconds(10)};
        _circuitBreaker = new CircuitBreaker(5, TimeSpan.FromSeconds(10));

        _flushTimer = new Timer(
            async _ => await FlushAsync(), 
            null, 
            TimeSpan.FromSeconds(flushIntervalSeconds), 
            TimeSpan.FromSeconds(flushIntervalSeconds)
            );
    }
    
    public async Task WriteAsync(TraceEntry entry)
    {
        await _semaphore.WaitAsync();

        try
        {
            _entries.Add(entry);
            
            if (entry.Level == TraceLevel.Fatal || _entries.Count >= _batchSize)
            {
                await FlushInternalAsync();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task FlushAsync()
    {
        await _semaphore.WaitAsync();
        
        try
        {
            await FlushInternalAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task FlushInternalAsync()
    {
        if (_entries.Count == 0) return;
        
        if (!_circuitBreaker.CanExecute())
        {
            System.Console.WriteLine("[RemoteSink] Circuit breaker is OPEN. Skipping remote log.");
            // _entries.Clear(); // or write to callbacks?
            return;
        }
        
        var tracesToSend = new List<TraceEntry>(_entries);
        _entries.Clear();

        int maxRetries = 3;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var json = JsonSerializer.Serialize(tracesToSend);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
 
                var response = await _httpClient.PostAsync(_endpoint, content);
                response.EnsureSuccessStatusCode();
                _circuitBreaker.RecordSuccess();
                
                System.Console.WriteLine($"[RemoteSink] Successfully sent {tracesToSend.Count} logs to {_endpoint}");
                return;
            }
            catch (Exception ex)
            {
                _circuitBreaker.RecordFailure();
                System.Console.WriteLine($"[RemoteSink] Attempt {attempt} failed: {ex.Message}");

                if (attempt < maxRetries)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt))); // Exponential backoff
                }
            }
        }
        
        System.Console.WriteLine($"[RemoteSink] Failed to send logs after {maxRetries} attempts.");
    }

    public void Dispose()
    {
        _flushTimer?.Dispose();
        _httpClient?.Dispose();
        _semaphore?.Dispose();
    }
}