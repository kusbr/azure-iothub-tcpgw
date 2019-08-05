using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using SocketIoT.Core.Common;
using SocketIoT.Core.Tcp.Packets;
using SocketIoT.Core.Tcp.Tenancy;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;

namespace SocketIoT.Core.Tcp.Codec
{
    public sealed class DeviceTopicDecoder : ByteToMessageDecoder
    {
        #region Static Constants and Class members
        public static readonly string HandlerName = "DeviceTopicDecoder";

        readonly IDeviceCredentialProvider deviceCredentialProvider;
        readonly IList<UriPathTemplate> topicTemplates;
        AbstractTenancyContext tenancyContext;
        #endregion

        /// <summary>
        /// Sets the Azure IoT Hub connection context using the tenancy context
        /// </summary>
        /// <param name="deviceCredentialProvider">Provides the client credentials</param>
        /// <param name="topicTemplates">Provides messaging topic templates for D2C, C2D and other IoT Hub endpoints </param>
        public DeviceTopicDecoder(IDeviceCredentialProvider deviceCredentialProvider, IList<UriPathTemplate> topicTemplates )
        {
            Contract.Requires(deviceCredentialProvider != null);
            this.deviceCredentialProvider = deviceCredentialProvider;
            this.topicTemplates = topicTemplates;
        }

        #region Decoder overrides
        protected override async void Decode(IChannelHandlerContext context, IByteBuffer input, List<object> output)
        {
            try
            {
                tenancyContext = context.GetAttribute<AbstractTenancyContext>(AttributeKey<AbstractTenancyContext>.ValueOf(AbstractTenancyContext.TENANCY_CONTEXT_KEY)).Get();
                var dataPacket = await tenancyContext?.Decode(input);
                dataPacket.EventTopicAddress = GetDeviceD2CAddress(dataPacket.DeviceId);
                var connectPacket = await GetDeviceCredentialAsync(tenancyContext.TenantId, dataPacket.DeviceId);
                output.Add(new Dictionary<PacketType, Packet>() { { PacketType.CONNECT, connectPacket }, { PacketType.D2C, dataPacket } });
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }

        protected override void DecodeLast(IChannelHandlerContext context, IByteBuffer input, List<object> output)
        {
            base.DecodeLast(context, input, output);
        }
        #endregion

        #region Credential and Topic Helpers
        async Task<ConnectPacket> GetDeviceCredentialAsync(string tenantId, string deviceId)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                return null;

            IDeviceCredential deviceCred = await this.deviceCredentialProvider.GetCredentialAsync(tenantId, deviceId);

            if (deviceCred == null)
                return null ;

            return new ConnectPacket(deviceId, deviceCred.Username, deviceCred.Password, 120); //TODO: Change session aliveness to config
        }

        string GetDeviceD2CAddress(string deviceId)
        {
            IDictionary<string, string> messageProperties = new Dictionary<string, string>() { { "deviceId", deviceId } };
            string D2CAddress = "";
            for (int i= 0; i < this.topicTemplates.Count(); i++)
            {
                try
                {
                    D2CAddress = this.topicTemplates[i].Bind( messageProperties );
                    if (!string.IsNullOrEmpty(D2CAddress))
                        break;
                }
                catch { }
            };
            return D2CAddress;
        }
        #endregion
    }
}
