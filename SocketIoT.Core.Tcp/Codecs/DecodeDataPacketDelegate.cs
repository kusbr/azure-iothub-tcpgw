using DotNetty.Buffers;
using SocketIoT.Core.Tcp.Packets;
using System.Threading.Tasks;

namespace SocketIoT.Core.Tcp.Codec
{
    public delegate Task<DeviceDataPacket> DecodeDataPacketDelegate(IByteBuffer input);
}
