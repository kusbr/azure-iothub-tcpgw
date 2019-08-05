namespace SocketIoT.Core.Tcp.Messaging
{
    using Microsoft.Azure.Devices.ProtocolGateway.Identity;
    using SocketIoT.Core.Common;
    using System;
    using System.Threading.Tasks;

    public sealed class SingleClientMessagingBridge : ITcpIoTHubMessagingBridge
    {
        readonly IDeviceIdentity deviceIdentity;
        readonly ITcpIoTHubMessagingServiceClient messagingClient;
        ITcpIoTHubMessagingChannel<ITcpMessage> messagingChannel;

        public SingleClientMessagingBridge(IDeviceIdentity deviceIdentity, ITcpIoTHubMessagingServiceClient messagingClient)
        {
            this.deviceIdentity = deviceIdentity;
            this.messagingClient = messagingClient;
        }

        public void BindMessagingChannel(ITcpIoTHubMessagingChannel<ITcpMessage> channel)
        {
            this.messagingChannel = channel;
        }

        public bool TryResolveClient(string topicName, out ITcpIoTHubMessagingServiceClient sendingClient)
        {
            sendingClient = this.messagingClient;
            return true;
        }

        public Task DisposeAsync(Exception cause)
        {
            if (cause == null)
            {
                //CommonEventSource.Log.Info($"Closing connection for device: {this.deviceIdentity}", string.Empty, string.Empty);
            }
            else
            {
                //string operationScope = cause.Data[MqttAdapter.OperationScopeExceptionDataKey]?.ToString();
                //string connectionScope = cause.Data[MqttAdapter.ConnectionScopeExceptionDataKey]?.ToString();
                //CommonEventSource.Log.Warning($"Closing connection for device: {this.deviceIdentity}" + (operationScope == null ? null : ", scope: " + operationScope), cause, connectionScope);
            }

            return this.messagingClient.DisposeAsync(cause);
        }

   
    }
}