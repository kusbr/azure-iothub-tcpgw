using SocketIoT.Core.Common;
using System.Collections;
using System.Threading.Tasks;

namespace SocketIoT
{
    public class InMemoryDeviceCredentialProvider : IDeviceCredentialProvider
    {
        Hashtable deviceCredentialMap = new Hashtable();

        public InMemoryDeviceCredentialProvider()
        {
            deviceCredentialMap["leafdevicename"] = new DeviceCredential
                (
                    "{deviceId_of_the_leaf_device_in_iothub}", "{iothubhostname}/{deviceid}", "{devicepassword}"
                );

        }

        public Task<IDeviceCredential> GetCredentialAsync(string tenantId, string deviceId)
        {
            return Task.FromResult<IDeviceCredential>(deviceCredentialMap[string.Format("{0}.{1}", tenantId, deviceId)] as IDeviceCredential);
        }

    }
}
