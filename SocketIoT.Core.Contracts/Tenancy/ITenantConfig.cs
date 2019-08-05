using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace SocketIoT.Core.Common.Tenancy
{
    public interface ITenantConfig
    {
        string TenantId { get; }

        string IoTHubConnectionString { get; }

        Hashtable DeviceConnectionString { get; }

        int MaxPendingInboundMessages { get; }

        int MaxPendingOutboundMessages { get; }

        int MaxOutboundRetransmissionCount { get; }
  
        int IoTConnectionPoolSize { get; }

        string IoTConnectionIdleTimeout { get; }

    }
}
