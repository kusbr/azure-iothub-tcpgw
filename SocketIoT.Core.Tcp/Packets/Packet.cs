using System.Diagnostics.Contracts;

namespace SocketIoT.Core.Tcp.Packets
{
    public abstract class Packet
    {
        public Packet(string deviceId)
        {
            Contract.Requires(!string.IsNullOrEmpty(deviceId) && !string.IsNullOrWhiteSpace(deviceId));
            this.DeviceId = deviceId;
        }

        public string DeviceId { get; }

        public abstract PacketType PacketType { get; }
    }
}
