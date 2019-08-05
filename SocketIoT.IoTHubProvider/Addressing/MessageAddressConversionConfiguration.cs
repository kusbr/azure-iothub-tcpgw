namespace SocketIoT.IoTHubProvider.Addressing
{
    using System.Collections.Generic;

    public sealed class MessageAddressConversionConfiguration
    {
        public MessageAddressConversionConfiguration()
        {
            this.InboundTemplates = new List<string>();
            this.OutboundTemplates = new List<string>();
        }

        internal MessageAddressConversionConfiguration(List<string> inboundTemplates, List<string> outboundTemplates)
        {
            this.InboundTemplates = inboundTemplates;
            this.OutboundTemplates = outboundTemplates;
        }

        public List<string> InboundTemplates { get; private set; }

        public List<string> OutboundTemplates { get; private set; }
    }
}