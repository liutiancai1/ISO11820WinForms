using ISO11820WinForms.Utilities;
using Xunit;

namespace ISO11820WinForms.Tests;

public class SerialPortCoordinatorTests
{
    [Fact]
    public async Task RunExclusive_WhenPortNamesEquivalent_ExecutesSequentially()
    {
        var steps = new List<string>();
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondEntered = false;

        var first = Task.Run(() =>
            SerialPortCoordinator.RunExclusive("com9", () =>
            {
                steps.Add("first-enter");
                firstEntered.SetResult();
                releaseFirst.Task.GetAwaiter().GetResult();
                steps.Add("first-exit");
            }));

        await firstEntered.Task;

        var second = Task.Run(() =>
            SerialPortCoordinator.RunExclusive("COM9", () =>
            {
                secondEntered = true;
                steps.Add("second-enter");
            }));

        await Task.Delay(100);

        Assert.False(secondEntered);

        releaseFirst.SetResult();
        await Task.WhenAll(first, second);

        Assert.Equal(new[] { "first-enter", "first-exit", "second-enter" }, steps);
    }
}
