using DotNetty.Transport.Channels;
using SocketIoT.Core.Common;
using SocketIoT.Core.Tcp.Tenancy;

namespace SocketIoT.Tenancy
{
    public sealed class TenancyContextFactory
    {
        public static AbstractTenancyContext GetContext(string name, string id, object trustInfo, IChannelHandlerContext context, ISettingsProvider settingsProvider)
        {
            switch (name)
            {
                case "O=Internet Widgits Pty Ltd, S=Some-State, C=IN":
                    return new TenantOneContext(new TenantInfo { TenantId = id, TenantName = name, TenantTrustInfo = trustInfo }, context, settingsProvider);

                case "CN=sensors.hydroponics.showcases.mtc.blr.in, O=MTC, S=Some-State, C=IN":
                    return new MTCShowcaseContext(new TenantInfo { TenantId = id, TenantName = name, TenantTrustInfo = trustInfo }, context, settingsProvider);

                case "CN=sensors.hydroponics.showcases.mtc.blr.in, OU=MTC, O=MTC, S=Some-State, C=IN":
                    return new MTCShowcaseContext(new TenantInfo { TenantId = id, TenantName = name, TenantTrustInfo = trustInfo }, context, settingsProvider);

                default:
                    return new DelayedTenancyContext(context, settingsProvider);
            }
        }
    }
}
