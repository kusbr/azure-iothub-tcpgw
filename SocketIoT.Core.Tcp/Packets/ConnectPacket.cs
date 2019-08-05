namespace SocketIoT.Core.Tcp.Packets
{
    public sealed class ConnectPacket : Packet
    {
        public ConnectPacket(string deviceId, string userName, string password, int keepAliveSeconds) : base(deviceId)
        {
            this.Username = userName;
            this.Password = password;
            this.KeepAliveSeconds = keepAliveSeconds;
        }
        public override PacketType PacketType => PacketType.CONNECT;
        public string Username { get; }
        public string Password { get; }
        public int KeepAliveSeconds { get; }
        public bool CleanSession { get; }
    }
}
