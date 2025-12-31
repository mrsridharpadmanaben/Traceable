namespace Traceable.Core.Sinks.Remote;

public class CircuitBreaker
{
    private enum CircuitState
    {
        Closed,
        Open,
        HalfOpen
    }

    private readonly int _failureThreshold;
    private readonly TimeSpan _timeout;

    private int _failureCount;
    private DateTime _lastFailureTime;
    private CircuitState _state = CircuitState.Closed;

    public CircuitBreaker(int  failureThreshold, TimeSpan timeout)
    {
        _failureThreshold = failureThreshold;
        _timeout = timeout;
    }

    public bool CanExecute()
    {
        switch (_state)
        {
            case CircuitState.Closed:
                return true;
            case CircuitState.Open when DateTime.Now - _lastFailureTime > _timeout:
                _state = CircuitState.HalfOpen;
                return true;
            case CircuitState.Open:
                return false;
            case CircuitState.HalfOpen:
                return true;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void RecordSuccess()
    {
        _failureCount = 0;
        _state = CircuitState.Closed;
    }

    public void RecordFailure()
    {
        _failureCount++;
        _lastFailureTime = DateTime.UtcNow;

        if (_failureCount >= _failureThreshold)
        {
            _state = CircuitState.Open;
        }
    }
}