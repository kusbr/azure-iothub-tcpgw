using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using Microsoft.Azure.Devices.ProtocolGateway.Identity;
using SocketIoT.AzureIoTHubClient;
using SocketIoT.Core.Common;
using SocketIoT.Core.Tcp.Codec;
using SocketIoT.IoTHubProvider;
using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;

namespace SocketIoT.Core.Tcp.Tenancy
{
    public abstract class AbstractTenancyContext
    {
        public static readonly string TENANCY_CONTEXT_KEY = "TENANCYCONTEXTKEY";

        readonly ISettingsProvider settingsProvider;

        public AttributeKey<AbstractTenancyContext> attributeKey = AttributeKey<AbstractTenancyContext>.ValueOf(TENANCY_CONTEXT_KEY);

        public AbstractTenancyContext(ISettingsProvider settingsProvider, TenantInfo tenancyData, IChannelHandlerContext channelContext, ChannelHandlerAdapter formatDecoder)
        {
            Contract.Requires(settingsProvider != null);
            this.settingsProvider = settingsProvider;

            this.TenantName = tenancyData?.TenantName;
            this.TenantId = tenancyData?.TenantId?.ToString();
            this.TenantTrustInfo = tenancyData?.TenantTrustInfo;
            this.ChannelContext = channelContext;

            if (channelContext.Channel.Pipeline.Get("TenantDataFormatDecoder") == null)
                channelContext.Channel.Pipeline.AddBefore(DeviceTopicDecoder.HandlerName, "TenantDataFormatDecoder", formatDecoder);

            this.ChannelContext?.GetAttribute(this.attributeKey).Set(this);
        }

        public abstract DecodeDataPacketDelegate Decode { get; }

        public object TenantTrustInfo { get; private set; }

        public string TenantName { get; private set; }

        public string TenantId { get; private set; }

        public Messaging.MessagingBridgeFactoryFunc IotBridgeFactory
        {
            get
            {
                const int DefaultConnectionPoolSize = 400; // IoT Hub default connection pool size
                TimeSpan DefaultConnectionIdleTimeout = TimeSpan.FromSeconds(210); // IoT Hub default connection idle timeout

                var iotHubClientSettings = new IotHubClientSettings(this.settingsProvider, this.TenantId);

                int connectionPoolSize = this.settingsProvider.GetIntegerSetting("IotHubClient.ConnectionPoolSize", DefaultConnectionPoolSize);
                TimeSpan connectionIdleTimeout = this.settingsProvider.GetTimeSpanSetting("IotHubClient.ConnectionIdleTimeout", DefaultConnectionIdleTimeout);
                string connectionString = iotHubClientSettings.IotHubConnectionString;

                int maxInboundMessageSize = this.settingsProvider.GetIntegerSetting("MaxInboundMessageSize", 256 * 1024);


                Func<IDeviceIdentity, Task<ITcpIoTHubMessagingServiceClient>> deviceClientFactory
                            = IotHubClient.PreparePoolFactory(connectionString, connectionPoolSize, connectionIdleTimeout, iotHubClientSettings);

                return async deviceIdentity => new Core.Tcp.Messaging.SingleClientMessagingBridge(deviceIdentity, await deviceClientFactory(deviceIdentity));

            }
        }

        public IChannelHandlerContext ChannelContext { get; private set; }

        public ChannelHandlerAdapter FormatDecoder { get; private set; }

        protected virtual void DelayedContext(TenantInfo tenantInfo)
        {
            this.TenantId = tenantInfo.TenantId.ToString();
            this.TenantName = tenantInfo.TenantName;
            this.TenantTrustInfo = tenantInfo.TenantTrustInfo;
        }


    }



}
