namespace SocketIoT.Core.Common
{
    using System;
    using System.Threading.Tasks;

    public interface ITcpIoTHubMessagingBridge
    {
        bool TryResolveClient(string topicName, out ITcpIoTHubMessagingServiceClient sendingClient);

        Task DisposeAsync(Exception cause);
    }
}