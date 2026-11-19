namespace LaraLauncher.Infrastructure.Windows;

/// <summary>
/// Ensures only one LaraLauncher instance runs per Windows user session and signals
/// the primary instance to restore focus if launched a second time.
/// </summary>
public sealed class SingleInstanceGuard : IDisposable
{
    private const string MutexName = @"Local\LaraLauncher_SingleInstance_Mutex";
    private const string ActivateEventName = @"Local\LaraLauncher_Activate_Event";

    private readonly Mutex _mutex;
    private readonly EventWaitHandle _activateEvent;
    private CancellationTokenSource? _listenerCts;

    public bool IsPrimaryInstance { get; }

    public event EventHandler? SecondInstanceSignaled;

    public SingleInstanceGuard()
    {
        _mutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
        IsPrimaryInstance = createdNew;
        _activateEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ActivateEventName);
    }

    public void SignalPrimaryInstance()
    {
        try
        {
            _activateEvent.Set();
        }
        catch (Exception)
        {
            // Best-effort wakeup signal.
        }
    }

    public void StartListeningForSecondInstances()
    {
        if (!IsPrimaryInstance || _listenerCts is not null)
        {
            return;
        }

        _listenerCts = new CancellationTokenSource();
        var token = _listenerCts.Token;

        _ = Task.Run(() =>
        {
            WaitHandle[] handles = [_activateEvent, token.WaitHandle];
            while (!token.IsCancellationRequested)
            {
                int signaled = WaitHandle.WaitAny(handles);
                if (signaled == 0 && !token.IsCancellationRequested)
                {
                    SecondInstanceSignaled?.Invoke(this, EventArgs.Empty);
                }
            }
        }, token);
    }

    public void Dispose()
    {
        if (_listenerCts is not null)
        {
            _listenerCts.Cancel();
            _listenerCts.Dispose();
            _listenerCts = null;
        }

        _activateEvent.Dispose();

        if (IsPrimaryInstance)
        {
            try { _mutex.ReleaseMutex(); } catch (ApplicationException) { }
        }

        _mutex.Dispose();
    }
}
