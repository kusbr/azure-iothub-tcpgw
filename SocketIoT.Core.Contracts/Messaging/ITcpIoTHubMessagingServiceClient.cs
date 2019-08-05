namespace SocketIoT.Core.Common
{
    using System;
    using System.Threading.Tasks;
    using DotNetty.Buffers;
    using Microsoft.Azure.Devices.ProtocolGateway.Messaging;

    public interface ITcpIoTHubMessagingServiceClient
    {
        IMessage CreateMessage(string address, IByteBuffer payload);

        Task SendAsync(IMessage message);

        Task AbandonAsync(string messageId);

        Task CompleteAsync(string messageId);

        Task RejectAsync(string messageId);

        Task DisposeAsync(Exception cause);
    }
}