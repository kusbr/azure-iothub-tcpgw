
using SocketIoT.Core.Common;

namespace SocketIoT
{
    public sealed class DeviceCredential : IDeviceCredential
    {
        public DeviceCredential(string deviceId, string Username, string SasRoken)
        {
            this.DeviceId = deviceId;
            this.Username = Username;
            this.SasToken = SasRoken;
        }

        public DeviceCredentialType credentialType => DeviceCredentialType.Sas;

        public string DeviceId { get; }

        public string Username { get; }

        public string SasToken { get; }
    }
}
