using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;

namespace Corel.Automation;

/// <summary>
/// Hosts a single dedicated STA thread that every COM call in a CLI invocation, REPL
/// session, or script run is dispatched through - one thread for the whole session, not
/// one per call, so the same RCWs stay valid across a sequence of commands.
/// </summary>
public sealed class StaThreadHost : IDisposable
{
    private readonly BlockingCollection<Action> _queue = [];
    private readonly Thread _thread;

    public StaThreadHost()
    {
        _thread = new Thread(RunLoop) { IsBackground = true };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
    }

    private void RunLoop()
    {
        foreach (Action action in _queue.GetConsumingEnumerable())
        {
            action();
        }
    }

    public T Invoke<T>(Func<T> func)
    {
        T result = default!;
        Exception? error = null;
        using var done = new ManualResetEventSlim(false);

        _queue.Add(() =>
        {
            try
            {
                result = func();
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                done.Set();
            }
        });

        done.Wait();
        if (error is not null)
        {
            ExceptionDispatchInfo.Capture(error).Throw();
        }
        return result;
    }

    public void Invoke(Action action) => Invoke<object?>(() =>
    {
        action();
        return null;
    });

    public void Dispose()
    {
        _queue.CompleteAdding();
        _thread.Join();
        _queue.Dispose();
    }
}
