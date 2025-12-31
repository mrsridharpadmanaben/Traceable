using Traceable.Core.Models;

namespace Traceable.Core.Sinks.Console;

public class ConsoleSink: ITraceSink
{
    private readonly bool _useJson;
    
    public ConsoleSink(bool useJson = false)
    {
        _useJson = useJson;
    }
    
    public Task WriteAsync(TraceEntry entry)
    {
        if (_useJson)
        {
            System.Console.WriteLine(entry.ToJson());
            
            return Task.CompletedTask;
        }

        var color = entry.Level switch
        {
            TraceLevel.Fatal => ConsoleColor.DarkRed,
            TraceLevel.Error => ConsoleColor.Red,
            TraceLevel.Warning => ConsoleColor.Yellow,
            TraceLevel.Info => ConsoleColor.Green,
            TraceLevel.Debug => ConsoleColor.Cyan,
            TraceLevel.Trace => ConsoleColor.Gray,
            _ => ConsoleColor.White
        };
        
        System.Console.ForegroundColor = color;
        System.Console.WriteLine(entry.ToJson());
        System.Console.ResetColor();
        
        return Task.CompletedTask;
    }

    public Task FlushAsync()
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
    }
}