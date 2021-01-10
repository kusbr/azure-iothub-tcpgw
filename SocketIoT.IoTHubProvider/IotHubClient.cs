namespace SocketIoT.AzureIoTHubClient
{
    using DotNetty.Buffers;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Exceptions;
    using Microsoft.Azure.Devices.ProtocolGateway.Identity;
    using Microsoft.Azure.Devices.ProtocolGateway.Messaging;
    using SocketIoT.Core.Common;
    using SocketIoT.IoTHubProvider;
    using System;
    using System.Threading.Tasks;
    using AzureIoTHUb = Microsoft.Azure.Devices.ProtocolGateway.IotHubClient;

    public class IotHubClient : ITcpIoTHubMessagingServiceClient
    {
        readonly DeviceClient deviceClient;
        readonly string deviceId;
        readonly IotHubClientSettings settings;

        IotHubClient(DeviceClient deviceClient, string deviceId, IotHubClientSettings settings)
        {
            this.deviceClient = deviceClient;
            this.deviceId = deviceId;
            this.settings = settings;
        }

        public static async Task<ITcpIoTHubMessagingServiceClient> CreateFromConnectionStringAsync(string deviceId, string connectionString,
            int connectionPoolSize, TimeSpan? connectionIdleTimeout, IotHubClientSettings settings)
        {
            int maxPendingOutboundMessages = settings.MaxPendingOutboundMessages;
            var tcpSettings = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only);
            var webSocketSettings = new AmqpTransportSettings(TransportType.Amqp_WebSocket_Only);
            webSocketSettings.PrefetchCount = tcpSettings.PrefetchCount = (uint)maxPendingOutboundMessages;
            if (connectionPoolSize > 0)
            {
                var amqpConnectionPoolSettings = new AmqpConnectionPoolSettings
                {
                    MaxPoolSize = unchecked ((uint)connectionPoolSize),
                    Pooling = connectionPoolSize > 0
                };
                if (connectionIdleTimeout.HasValue)
                {
                    amqpConnectionPoolSettings.ConnectionIdleTimeout = connectionIdleTimeout.Value;
                }
                tcpSettings.AmqpConnectionPoolSettings = amqpConnectionPoolSettings;
                webSocketSettings.AmqpConnectionPoolSettings = amqpConnectionPoolSettings;
            }
            DeviceClient client = DeviceClient.CreateFromConnectionString(connectionString, new ITransportSettings[]
            {
                tcpSettings,
                webSocketSettings
            });
            try
            {
                await client.OpenAsync();
            }
            catch (IotHubException ex)
            {
                throw ComposeIotHubCommunicationException(ex);
            }
            return new IotHubClient(client, deviceId, settings);
        }

        public static Func<IDeviceIdentity, Task<ITcpIoTHubMessagingServiceClient>> PreparePoolFactory(string baseConnectionString, int connectionPoolSize,
            TimeSpan? connectionIdleTimeout, IotHubClientSettings settings)
        {
            Func<IDeviceIdentity, Task<ITcpIoTHubMessagingServiceClient>> mqttCommunicatorFactory = deviceIdentity =>
            {
                IotHubConnectionStringBuilder csb = IotHubConnectionStringBuilder.Create(baseConnectionString);
                var identity = (AzureIoTHUb.IotHubDeviceIdentity)deviceIdentity;
                csb.AuthenticationMethod = DeriveAuthenticationMethod(csb.AuthenticationMethod, identity);
                csb.HostName = identity.IotHubHostName;
                string connectionString = csb.ToString();
                return CreateFromConnectionStringAsync(identity.Id, connectionString, connectionPoolSize, connectionIdleTimeout, settings);
            };
            return mqttCommunicatorFactory;
        }

        public string DeviceId => deviceId;

        public IMessage CreateMessage(string address, IByteBuffer payload)
        {
            var message = new IotHubClientMessage(new Message(payload.IsReadable() ? new ReadOnlyByteBufferStream(payload, false) : null), payload);
            message.Address = address;
            return message;
        }

        public async Task SendAsync(IMessage message)
        {
            var clientMessage = (IotHubClientMessage)message;
            try
            {
                string address = message.Address;
                Message iotHubMessage = clientMessage.ToMessage();
                await this.deviceClient.SendEventAsync(iotHubMessage);
            }
            catch (IotHubException ex)
            {
                throw ComposeIotHubCommunicationException(ex);
            }
            finally
            {
                clientMessage.Dispose();
            }
        }

        public async Task AbandonAsync(string messageId)
        {
            try
            {
                await this.deviceClient.AbandonAsync(messageId);
            }
            catch (IotHubException ex)
            {
                throw ComposeIotHubCommunicationException(ex);
            }
        }

        public async Task CompleteAsync(string messageId)
        {
            try
            {
                await this.deviceClient.CompleteAsync(messageId);
            }
            catch (IotHubException ex)
            {
                throw ComposeIotHubCommunicationException(ex);
            }
        }

        public async Task RejectAsync(string messageId)
        {
            try
            {
                await this.deviceClient.RejectAsync(messageId);
            }
            catch (IotHubException ex)
            {
                throw ComposeIotHubCommunicationException(ex);
            }
        }

        public async Task DisposeAsync(Exception cause)
        {
            try
            {
                await this.deviceClient.CloseAsync();
            }
            catch (IotHubException ex)
            {
                throw ComposeIotHubCommunicationException(ex);
            }
        }

        public async Task CloseAsync()
        {
            await this.deviceClient.CloseAsync();
        }

        internal static IAuthenticationMethod DeriveAuthenticationMethod(IAuthenticationMethod currentAuthenticationMethod, AzureIoTHUb.IotHubDeviceIdentity deviceIdentity)
        {
            switch (deviceIdentity.Scope)
            {
                case AzureIoTHUb.AuthenticationScope.None:
                    var policyKeyAuth = currentAuthenticationMethod as DeviceAuthenticationWithSharedAccessPolicyKey;
                    if (policyKeyAuth != null)
                    {
                        return new DeviceAuthenticationWithSharedAccessPolicyKey(deviceIdentity.Id, policyKeyAuth.PolicyName, policyKeyAuth.Key);
                    }
                    var deviceKeyAuth = currentAuthenticationMethod as DeviceAuthenticationWithRegistrySymmetricKey;
                    if (deviceKeyAuth != null)
                    {
                        return new DeviceAuthenticationWithRegistrySymmetricKey(deviceIdentity.Id, deviceKeyAuth.DeviceId);
                    }
                    var deviceTokenAuth = currentAuthenticationMethod as DeviceAuthenticationWithToken;
                    if (deviceTokenAuth != null)
                    {
                        return new DeviceAuthenticationWithToken(deviceIdentity.Id, deviceTokenAuth.Token);
                    }
                    throw new InvalidOperationException("");
                case AzureIoTHUb.AuthenticationScope.SasToken:
                    return new DeviceAuthenticationWithToken(deviceIdentity.Id, deviceIdentity.Secret);
                case AzureIoTHUb.AuthenticationScope.DeviceKey:
                    return new DeviceAuthenticationWithRegistrySymmetricKey(deviceIdentity.Id, deviceIdentity.Secret);
                case AzureIoTHUb.AuthenticationScope.HubKey:
                    return new DeviceAuthenticationWithSharedAccessPolicyKey(deviceIdentity.Id, deviceIdentity.PolicyName, deviceIdentity.Secret);
                default:
                    throw new InvalidOperationException("Unexpected AuthenticationScope value: " + deviceIdentity.Scope);
            }
        }

        static MessagingException ComposeIotHubCommunicationException(IotHubException ex)
        {
            return new MessagingException(ex.Message, ex.InnerException, ex.IsTransient, ex.TrackingId);
        }

        IMessage ITcpIoTHubMessagingServiceClient.CreateMessage(string address, IByteBuffer payload)
        {
            var message = new IotHubClientMessage(new Message(payload.IsReadable() ? new ReadOnlyByteBufferStream(payload, false) : null), payload);
            message.Address = address;
            return message;
        }
    }
}