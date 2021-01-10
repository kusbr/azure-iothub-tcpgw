using SocketIoT.Core.Common;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SocketIoT
{
    public class InMemoryDeviceCredentialProvider : IDeviceCredentialProvider
    {
        Hashtable deviceCredentialMap = new Hashtable();

        public InMemoryDeviceCredentialProvider(IList<TenantConfig> tenantConfig)
        {
            foreach (var tenant in tenantConfig)
            {
                var tenantId = tenant.TenantId;
                foreach (var device in tenant.Devices)
                {
                    var deviceCredential = new DeviceCredential(device.Id, $"{device.IoTHubHostName}/{device.Id}", device.SasToken);
                    deviceCredentialMap.Add($"{tenantId}.{device.Id}", deviceCredential);
                }
            }
        }

        public Task<IDeviceCredential> GetCredentialAsync(string tenantId, string deviceId)
        {
            return Task.FromResult<IDeviceCredential>(deviceCredentialMap[string.Format("{0}.{1}", tenantId, deviceId)] as IDeviceCredential);
        }

    }
}
