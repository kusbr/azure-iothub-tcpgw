namespace SocketIoT.Core.Tcp.Packets
{
    public enum PacketType
    {
        REGISTER = 1,
        DPS_REGISTER = 2,
        CONNECT = 3,
        D2C = 4,
        C2D= 5,
        HEART_BEAT = 6,
        DEVCE_TWIN_UPDATE = 7,
        FILE_UPLOAD = 8,
        DISCONNECT = 9
    }
}
