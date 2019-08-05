namespace SocketIoT.Core.Common
{
    public interface IDeviceCredential
    {
        DeviceCredentialType credentialType { get; }
        string DeviceId { get; }
        string Username { get; }
        string Password{ get; }
    }
}
