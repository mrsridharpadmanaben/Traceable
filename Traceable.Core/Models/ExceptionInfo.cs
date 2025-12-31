namespace Traceable.Core.Models;

public class ExceptionInfo
{
    public string? Type { get; set; }
    public string? Message { get; set; }
    public string? StackTrace { get; set; }
    public ExceptionInfo? InnerException { get; set; }

    public static ExceptionInfo? FromException(Exception? ex)
    {
        if (ex == null) return null;

        return new ExceptionInfo
        {
            Type = ex.GetType().FullName,
            Message = ex.Message,
            StackTrace = ex.StackTrace,
            InnerException = FromException(ex.InnerException)
        };
    }
}