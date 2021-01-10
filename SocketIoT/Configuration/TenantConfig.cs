using SocketIoT.Configuration;
using System.Collections.Generic;

namespace SocketIoT
{
    public sealed class TenantConfig
    {
        public string TenantId { get; set; }

        public IList<DeviceConfig> Devices { get; set; }
    }
}
