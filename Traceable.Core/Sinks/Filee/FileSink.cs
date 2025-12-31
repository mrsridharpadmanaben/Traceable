using Traceable.Core.Models;

namespace Traceable.Core.Sinks.Filee;

public class FileSink: ITraceSink
{
    private readonly string _filePath;
    private readonly bool _useJson;
    private readonly long _maxFileSizeBytes;
    private readonly int _maxArchiveFiles;
    
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public FileSink(string filePath, bool useJson = false, long maxFileSizeMegaBytes = 10, int maxArchiveFiles = 5)
    {
        _filePath = filePath;
        _useJson = useJson;
        _maxFileSizeBytes = maxFileSizeMegaBytes * 1024 * 1024;
        _maxArchiveFiles = maxArchiveFiles;
        
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
    
    public async Task WriteAsync(TraceEntry entry)
    {
        await _semaphore.WaitAsync();

        try
        {
            string trace = string.Empty;

            if (_useJson)
            {
                trace = entry.ToJson();
            }
            else
            {
                trace = entry.ToPlainText();
            }
            
            await File.AppendAllTextAsync(_filePath, trace);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task RotateFileIfNecessary()
    {
        if(!File.Exists(_filePath)) return;
        
        var fileInfo = new FileInfo(_filePath);
        if(fileInfo.Length < _maxFileSizeBytes) return;

        // Rotate files
        for (int i = _maxArchiveFiles - 1; i > 0; i--)
        {
            var oldFile = $"{_filePath}_{i}";
            var newFile = $"{_filePath}_{i + 1}";

            if (File.Exists(oldFile))
            {
                if(File.Exists(newFile)) File.Delete(newFile);
                
                File.Move(oldFile, newFile);
            }
        }
        
        // move current file to _1
        var archiveFile = $"{_filePath}.1";
        if (File.Exists(archiveFile))
            File.Delete(archiveFile);
        File.Move(_filePath, archiveFile);
    }

    public Task FlushAsync()
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _semaphore?.Dispose();
    }
}