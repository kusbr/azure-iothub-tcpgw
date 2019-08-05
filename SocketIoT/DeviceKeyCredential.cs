
using SocketIoT.Core.Common;

namespace SocketIoT
{
    public sealed class DeviceCredential : IDeviceCredential
    {
        public DeviceCredential(string deviceId, string Username, string Password)
        {
            this.DeviceId = deviceId;
            this.Username = Username;
            this.Password = Password;
        }

        public DeviceCredentialType credentialType => DeviceCredentialType.Sas;

        public string DeviceId { get; }

        public string Username { get; }

        public string Password { get; }
    }
}
