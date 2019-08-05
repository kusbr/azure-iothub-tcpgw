using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using Newtonsoft.Json.Linq;
using SocketIoT.Core.Common;
using SocketIoT.Core.Tcp.Codec;
using SocketIoT.Core.Tcp.Packets;
using System;
using System.Text;
using System.Threading.Tasks;

namespace SocketIoT.Core.Tcp.Tenancy
{
    public sealed class DelayedTenancyContext : AbstractTenancyContext
    {
        readonly ISettingsProvider settingsProvider;

        public DelayedTenancyContext(IChannelHandlerContext context, ISettingsProvider settingsProvider)
            : base(settingsProvider, null, context, new DelimiterBasedFrameDecoder(settingsProvider.GetIntegerSetting("MaxInboundMessageSize", 256 * 1024), true, Delimiters.LineDelimiter()))
        {
            this.settingsProvider = settingsProvider;
        }

        public override DecodeDataPacketDelegate Decode =>
            (IByteBuffer input) =>
            {
                try
                {
                    int TENANT_UNIQUEID_POS = 0;
                    int TS_TOKEN_POS = 1;
                    int DEVICEID_TOKEN_POS = 2;
                    int DATASTART_POS = 3;

                    byte[] buffer = new byte[input.ReadableBytes];
                    input.ReadBytes(buffer);
                    var packetStr = Encoding.UTF8.GetString(buffer);
                    var delim = '~';

                    var tokens = packetStr.Split(delim, StringSplitOptions.RemoveEmptyEntries);
                    string tenantId = "";
                    string tenantName = "";
                    if (tokens.Length > 0)
                    {
                        tenantId = tokens[TENANT_UNIQUEID_POS];
                        tenantName = "{Verify Tenant using SourceIp, Passed SrialID etc. and Lookup Tenant Name}";
                        object tenantTrust = "{Determine and build Tenant Trust object}";
                        this.DelayedContext(new TenantInfo { TenantId = tenantId, TenantName = tenantName, TenantTrustInfo = tenantTrust });
                    }

                    if (tokens?.Length >= 3)
                    {
                        string deviceId = tokens[DEVICEID_TOKEN_POS];
                        string timestamp = tokens[TS_TOKEN_POS];
                        string dataStr = string.Join(delim, tokens, DATASTART_POS, tokens.Length - DATASTART_POS);
                        var dataBuf = ByteBufferUtil.EncodeString(ByteBufferUtil.DefaultAllocator, dataStr, Encoding.UTF8);
                        return Task.FromResult(new DeviceDataPacket(deviceId, string.Empty, dataBuf));
                    }
                    else
                    {
                        return null;
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                    return null;
                }
            };

    }
}
