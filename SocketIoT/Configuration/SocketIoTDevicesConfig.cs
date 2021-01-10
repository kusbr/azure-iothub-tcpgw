using System.Collections.Generic;

namespace SocketIoT.Configuration
{
    public class SocketIoTDevicesConfig
    {
        public IList<TenantConfig> Tenants { get; set; }

        public string MessageDelimiter { get; set; }
    }
}
