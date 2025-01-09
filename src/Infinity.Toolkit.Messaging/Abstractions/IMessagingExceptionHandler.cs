namespace Infinity.Toolkit.Messaging.Abstractions;

public interface IMessagingExceptionHandler
{
    ValueTask<bool> TryHandleAsync(string channelName, Exception exception);
}
