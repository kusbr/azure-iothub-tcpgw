namespace SocketIoT.IoTHubProvider.Addressing
{
    using Microsoft.Azure.Devices.ProtocolGateway.Messaging;
    using SocketIoT.Core.Common;
    using System.Collections.Generic;

    public interface IMessageAddressConverter
    {
        IList<UriPathTemplate> TopicTemplates { get; }

        bool TryDeriveOutboundAddress(IMessage message, out string address);

        bool TryParseAddressIntoMessageProperties(string address, IMessage message);

    }
}