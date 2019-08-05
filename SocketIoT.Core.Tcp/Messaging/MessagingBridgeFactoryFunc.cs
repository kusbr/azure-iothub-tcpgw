
namespace SocketIoT.Core.Tcp.Messaging
{
    using Microsoft.Azure.Devices.ProtocolGateway.Identity;
    using SocketIoT.Core.Common;
    using System.Threading.Tasks;

    public delegate Task<ITcpIoTHubMessagingBridge> MessagingBridgeFactoryFunc(IDeviceIdentity deviceIdentity);
}