using System.Threading.Tasks;

namespace SocketIoT.Core.Common
{
    public interface IDeviceCredentialProvider
    {
        Task<IDeviceCredential> GetCredentialAsync(string tenantId, string deviceId);
    }
}
