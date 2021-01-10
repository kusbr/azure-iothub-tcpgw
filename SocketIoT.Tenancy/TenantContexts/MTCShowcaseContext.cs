using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using SocketIoT.Core.Common;
using SocketIoT.Core.Tcp.Codec;
using SocketIoT.Core.Tcp.Packets;
using SocketIoT.Core.Tcp.Tenancy;
using System;
using System.Text;
using System.Threading.Tasks;

namespace SocketIoT.Tenancy
{
    public sealed class MTCShowcaseContext : AbstractTenancyContext
    {
        readonly ISettingsProvider settingsProvider;

        public MTCShowcaseContext(TenantInfo tenantInfo, IChannelHandlerContext channelContext, ISettingsProvider settingsProvider) : 
            base(settingsProvider, tenantInfo, channelContext, new DelimiterBasedFrameDecoder(settingsProvider.GetIntegerSetting("MaxInboundMessageSize", 256 * 1024), true, Delimiters.LineDelimiter()))
        {
            this.settingsProvider = settingsProvider;
        }

        //Set tenant configuration specific decoder function logic (either here or as a provider)
        public override DecodeDataPacketDelegate Decode =>
            (IByteBuffer input) =>
            {
                try
                {
                    int TS_TOKEN_POS = 0;
                    int DEVICEID_TOKEN_POS = 1;
                    int DATASTART_POS = 2;

                    byte[] buffer = new byte[input.ReadableBytes];
                    input.ReadBytes(buffer);
                    var packetStr = Encoding.UTF8.GetString(buffer);
                    var delim = '~';

                    var tokens = packetStr.Split(delim, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens?.Length >= 3)
                    {
                        string deviceId = tokens[DEVICEID_TOKEN_POS];
                        string timestamp = tokens[TS_TOKEN_POS];
                        string dataStr = string.Join(delim, tokens, DATASTART_POS, tokens.Length - DATASTART_POS);

                        var dataJson = string.Format("{{'ts': '{0}', 'tenantId': '{1}', 'deviceId': '{2}', 'data':'{3}' }}", DateTime.UtcNow, "MTC Hydroponics", deviceId, dataStr ); 

                        var dataBuf = ByteBufferUtil.EncodeString(ByteBufferUtil.DefaultAllocator, dataJson, Encoding.UTF8);
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
