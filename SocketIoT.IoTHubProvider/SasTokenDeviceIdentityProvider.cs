namespace SocketIoT.IoTHubProvider
{
    using Microsoft.Azure.Devices.ProtocolGateway.Identity;
    using Microsoft.Azure.Devices.ProtocolGateway.IotHubClient;
    using System;
    using System.Net;
    using System.Threading.Tasks;

    public sealed class SasTokenDeviceIdentityProvider : IDeviceIdentityProvider
    {
        public Task<IDeviceIdentity> GetAsync(string clientId, string username, string password, EndPoint clientAddress)
        {
            IotHubDeviceIdentity deviceIdentity;
            if (!IotHubDeviceIdentity.TryParse(username, out deviceIdentity) || !clientId.Equals(deviceIdentity.Id, StringComparison.Ordinal))
            {
                return Task.FromResult(UnauthenticatedDeviceIdentity.Instance);
            }
            deviceIdentity.WithSasToken(password);
            return Task.FromResult<IDeviceIdentity>(deviceIdentity);
        }
    }
}