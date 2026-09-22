using Corel.Automation;

namespace Corel.Automation.Tests;

public class SaveDialogWatchdogTests
{
    [Fact]
    public void RunWithBackgroundWatcher_PropagatesActionResult_WhenWatcherNeverFaults()
    {
        bool ran = false;

        SaveDialogWatchdog.RunWithBackgroundWatcher(
            action: () => ran = true,
            watch: token => token.WaitHandle.WaitOne());

        Assert.True(ran);
    }

    [Fact]
    public void RunWithBackgroundWatcher_PropagatesActionsOriginalException_EvenWhenWatcherAlsoFaults()
    {
        var actionException = new InvalidOperationException("real SaveAs failure");

        InvalidOperationException thrown = Assert.Throws<InvalidOperationException>(() =>
            SaveDialogWatchdog.RunWithBackgroundWatcher(
                action: () => throw actionException,
                watch: _ => throw new InvalidDataException("watcher blew up")));

        Assert.Same(actionException, thrown);
    }

    [Fact]
    public void RunWithBackgroundWatcher_DoesNotThrow_WhenOnlyTheWatcherFaults()
    {
        var exception = Record.Exception(() =>
            SaveDialogWatchdog.RunWithBackgroundWatcher(
                action: () => { },
                watch: _ => throw new InvalidDataException("watcher blew up")));

        Assert.Null(exception);
    }

    [Fact]
    public void RunWithBackgroundWatcher_CancelsWatcherAfterActionCompletes()
    {
        bool observedCancellation = false;

        SaveDialogWatchdog.RunWithBackgroundWatcher(
            action: () => { },
            watch: token =>
            {
                token.WaitHandle.WaitOne(TimeSpan.FromSeconds(5));
                observedCancellation = token.IsCancellationRequested;
            });

        Assert.True(observedCancellation);
    }
}
