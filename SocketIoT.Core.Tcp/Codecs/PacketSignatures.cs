using SocketIoT.Core.Tcp.Packets;

namespace SocketIoT.Core.Tcp.Codec
{
    static class PacketSignatures
    {
        public const byte RegisterDevice = (int)PacketType.REGISTER << 4;
        public const byte DPSRegisterDevice = (int)PacketType.DPS_REGISTER << 4;
        public const byte ConnectDevice = (int)PacketType.CONNECT << 4;
        public const byte DeviceHeartBeat = (int)PacketType.HEART_BEAT<< 4;
        public const byte DeviceDataSend = (int)PacketType.D2C << 4;
        public const byte CloudDataSend = (int)PacketType.C2D << 4;
        public const byte DeviceTwinUpdate = (int)PacketType.DEVCE_TWIN_UPDATE << 4;
        public const byte DeviceFileUpload = (int)PacketType.FILE_UPLOAD << 4;
        public const byte DisconnectDevice = (int)PacketType.DISCONNECT << 4;
    }
}
