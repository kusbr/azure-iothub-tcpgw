namespace SocketIoT.Core.Common
{
    using System;

    public interface ITcpIoTHubMessagingChannel<TMessage>
    {
        void Handle(TMessage message);

        void Close(Exception cause);

        event EventHandler CapabilitiesChanged;
    }
}