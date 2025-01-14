using Infinity.Toolkit.Messaging.Abstractions;
using System.Diagnostics;

namespace Infinity.Toolkit.Tests.Messaging.InMemory;

internal static class MessagingTestHelpers
{
    public static Task<ObservedMessageContexts> SendAndWaitForEvent<T>(
        Func<Task> testAction,
        TimeSpan? timeout = null)
        where T : class
            => SendAndWaitForEvent<T>(
                testAction,
                m => m.MessageType == typeof(T),
                timeout);

    public static async Task<ObservedMessageContexts> SendAndWaitForEvent<T>(
        Func<Task> testAction,
        Func<IMessageHandlerContext<T>, bool> predicate,
        TimeSpan? timeout = null)
        where T : class
    {
        timeout = Debugger.IsAttached ? null : timeout ?? TimeSpan.FromSeconds(10);

        var incomingMessageContexts = new List<IMessageHandlerContext>();

        var messageReceivedTaskCompletionSource = new TaskCompletionSource<object>();

        using ActivityListener listener = new();
        listener.ActivityStopped = (activitySource) =>
        {
            if (activitySource.OperationName.Contains("consume"))
            {
                var context = activitySource.GetTagItem("testing.incoming.message.context") as IMessageHandlerContext<T>;

                incomingMessageContexts.Add(context!);

                if (context is IMessageHandlerContext<T> ctx && predicate(ctx))
                {
                    messageReceivedTaskCompletionSource.TrySetResult(default!);
                }
            }
        };

        listener.ShouldListenTo = _ => true;
        listener.Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData;
        ActivitySource.AddActivityListener(listener);

        await testAction();

        if (timeout.HasValue)
        {
            var timeoutTask = Task.Delay(timeout.Value);
            var finishedTask = await Task.WhenAny(messageReceivedTaskCompletionSource.Task, timeoutTask);
            if (finishedTask == timeoutTask)
            {
                throw new TimeoutException();
            }
        }
        else
        {
            await messageReceivedTaskCompletionSource.Task;
        }

        return new ObservedMessageContexts(incomingMessageContexts);
    }
}
