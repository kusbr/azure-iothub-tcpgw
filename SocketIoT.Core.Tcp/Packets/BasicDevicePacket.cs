using System.Collections.Generic;

namespace SocketIoT.Core.Tcp.Packets
{
    public class BasicDevicePacket : Packet
    {
        public override PacketType PacketType => PacketType.D2C;

        public BasicDevicePacket(string deviceId, IEnumerable<byte> decodedData) : base(deviceId)
        {
            this.DecodedData = decodedData;
        }

        public IEnumerable<byte> DecodedData { get; private set; }
    }
}
