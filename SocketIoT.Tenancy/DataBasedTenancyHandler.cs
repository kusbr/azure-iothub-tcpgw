using DotNetty.Transport.Channels;
using SocketIoT.Core.Common;
using SocketIoT.Core.Tcp.Tenancy;

namespace SocketIoT.Tenancy
{
    public sealed class DataBasedTenancyHandler : ChannelHandlerAdapter
    {
        public static readonly string HandlerName = "DataBasedTenancyHandler";

        ISettingsProvider settingsProvider;
        IChannelHandlerContext capturedContext;
        AbstractTenancyContext tenantContext;

        public DataBasedTenancyHandler(ISettingsProvider settingsProvider)
        {
            this.settingsProvider = settingsProvider;
        }

        public override void ChannelActive(IChannelHandlerContext context)  
        {
            this.capturedContext = context;
            base.ChannelActive(context);
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            this.SetupDelayedTenancyIdentifier();
            base.ChannelRead(context, message);
        }

        public override bool IsSharable => true;

        /// <summary>
        /// Use tenant identifier and create the appropriate context object 
        /// </summary>
        private void SetupDelayedTenancyIdentifier()
        {
            this.tenantContext = TenancyContextFactory.GetContext(null, null, null, this.capturedContext, this.settingsProvider);
        }
    }

    
}
