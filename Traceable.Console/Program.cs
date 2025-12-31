using Traceable.Core;
using Traceable.Core.Models;

Console.WriteLine("=== Logging Framework Demo ===\n");

// Example 1: Simple Console Logging
Console.WriteLine("--- Example 1: Console Logging ---");

var simpleLogger = new TracerConfiguration().SetServiceName("SimpleApp")
    .SetMinimumLevel(TraceLevel.Debug)
    .WriteToConsole()
    .CreateTracer();

simpleLogger.Debug("This is a debug message");
simpleLogger.Info("Application started successfully");
simpleLogger.Warning("This is a warning");
simpleLogger.Error("An error occurred",
    new Exception("Something went wrong"));
await Task.Delay(100);
Console.WriteLine();

// Example 2: Structured Logging (JSON)
Console.WriteLine("--- Example 2: Structured JSON Logging ---");
var jsonLogger = new TracerConfiguration().SetServiceName("OrderService")
    .SetMinimumLevel(TraceLevel.Info)
    .WriteToConsole(useJson: true)
    .CreateTracer();

jsonLogger.Info("Order created");
var contextLogger = jsonLogger.WithContext("OrderId",
        12345)
    .WithContext("CustomerId",
        67890)
    .WithContext("Amount",
        299.99m)
    .WithCorrelationId("req-abc-123");
contextLogger.Info("Order processed successfully");
await Task.Delay(100);
Console.WriteLine();

// Example 3: Multiple Sinks (Console + File + Database + Remote)
Console.WriteLine("--- Example 3: Multiple Sinks ---");
var multiLogger = new TracerConfiguration().SetServiceName("PaymentService")
    .SetMinimumLevel(TraceLevel.Trace)
    .WriteToConsole()
    .WriteToFile("logs/app.log",
        useJson: false,
        maxFileSizeMegaBytes: 5)
    .WriteToFile("logs/structured.json",
        useJson: true)
    .CreateTracer();

multiLogger.Info("Payment service initialized");

for (int i = 1; i <= 15; i++)
{
    multiLogger.WithContext("PaymentId", i)
        .WithContext("Status",
            "Completed")
        .Info($"Payment #{i} processed");
}

await Task.Delay(1000);
Console.WriteLine();

// Example 4: Error with Exception
Console.WriteLine("--- Example 4: Exception Logging ---");
try
{
    throw new InvalidOperationException("Database connection failed",
        new TimeoutException("Connection timeout after 30 seconds"));
}
catch (Exception ex)
{
    multiLogger.Error("Failed to connect to database",
        ex);
}

await Task.Delay(100);
Console.WriteLine();

// Example 5: Performance Test
Console.WriteLine("--- Example 5: Performance Test (10,000 logs) ---");
var perfLogger = new TracerConfiguration().SetServiceName("PerfTest")
    .SetMinimumLevel(TraceLevel.Info)
    .WriteToConsole()
    .CreateTracer();

var sw = System.Diagnostics.Stopwatch.StartNew();
for (int i = 0; i < 10000; i++)
{
    perfLogger.Info($"Log message #{i}");
}

sw.Stop();
Console.WriteLine($"Enqueued 10,000 logs in {sw.ElapsedMilliseconds}ms");
Console.WriteLine("Waiting for background processing...");
await Task.Delay(2000);
Console.WriteLine();

// Cleanup
Console.WriteLine("--- Flushing and Cleanup ---");
simpleLogger.Dispose();
jsonLogger.Dispose();
multiLogger.Dispose();
perfLogger.Dispose();