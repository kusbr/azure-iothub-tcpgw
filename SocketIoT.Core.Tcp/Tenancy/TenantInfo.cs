namespace SocketIoT.Core.Tcp.Tenancy
{
    public sealed class TenantInfo
    {
        public string TenantId { get; set; }

        public string TenantName { get; set; }

        public object TenantTrustInfo { get; set; }
    }
}
