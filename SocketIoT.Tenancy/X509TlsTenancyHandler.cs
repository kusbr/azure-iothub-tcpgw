using DotNetty.Common.Concurrency;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Channels;
using SocketIoT.Core.Common;
using SocketIoT.Core.Tcp;
using SocketIoT.Core.Tcp.Tenancy;
using System;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace SocketIoT.Tenancy
{
    public sealed class X509TlsTenancyHandler : IChannelHandler
    {
        readonly TlsHandler tlsHandler;
        IChannelHandlerContext capturedContext;
        AbstractTenancyContext tenantContext;
        readonly ISettingsProvider settingsProvider;
        readonly TaskCompletionSource closeFuture;

        public X509TlsTenancyHandler(ServerTlsSettings tlsSettings, ISettingsProvider settingsProvider)
        {
            Contract.Requires(tlsSettings != null);
            this.tlsHandler = new TlsHandler(stream => new SslStream(stream, true, ClientCertValidatorCallback), tlsSettings);
            this.closeFuture = new TaskCompletionSource();
            this.settingsProvider = settingsProvider;
        }

        private bool ClientCertValidatorCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            X509Certificate2 cert2 = null;
            try
            {
                bool clientVerified = sslPolicyErrors == SslPolicyErrors.None;
                return (clientVerified) && this.EstablishTenancyContext(cert2.Thumbprint, cert2.SubjectName.Name, chain);
            }
            catch
            {
                throw new SocketIoTGatewayException(
                    Core.Tcp.ErrorCode.UnResolvedSendingClient, $"Error occured while establishing tenancy using certificate- Issuer: {certificate.Issuer} Subject: {certificate.Subject}");
            }

        }

        /// <summary>
        /// Use tenant identifier and create the appropriate context object 
        /// </summary>
        private bool EstablishTenancyContext(string tenantId, string tenantName, X509Chain chain)
        {
            this.tenantContext = TenancyContextFactory.GetContext(tenantName, tenantId, chain, this.capturedContext, this.settingsProvider);
            return true;
        }
       
        #region IChannelAdapter Members
        public Task BindAsync(IChannelHandlerContext context, EndPoint localAddress)
        {
            return this.tlsHandler.BindAsync(context, localAddress);
        }

        public void ChannelActive(IChannelHandlerContext context)
        {
            this.capturedContext = context;
            this.tlsHandler.ChannelActive(context);
        }

        public void ChannelInactive(IChannelHandlerContext context)
        {
            this.tlsHandler.ChannelInactive(context);
        }

        public void ChannelRead(IChannelHandlerContext context, object message)
        {
            tlsHandler.ChannelRead(context, message);
        }

        public void ChannelReadComplete(IChannelHandlerContext context)
        {
            this.tlsHandler.ChannelReadComplete(context);
        }

        void ReadIfNeeded(IChannelHandlerContext ctx)
        {
           
        }

        public void ChannelRegistered(IChannelHandlerContext context)
        {
            this.tlsHandler.ChannelRegistered(context);
        }

        public void ChannelUnregistered(IChannelHandlerContext context)
        {
            this.tlsHandler.ChannelUnregistered(context);
        }

        public void ChannelWritabilityChanged(IChannelHandlerContext context)
        {
            this.tlsHandler.ChannelWritabilityChanged(context);
        }

        public Task CloseAsync(IChannelHandlerContext context)
        {
            return this.tlsHandler.CloseAsync(context);
        }

        public Task ConnectAsync(IChannelHandlerContext context, EndPoint remoteAddress, EndPoint localAddress)
        {
            return this.tlsHandler.ConnectAsync(context, remoteAddress, localAddress);
        }

        public Task DeregisterAsync(IChannelHandlerContext context)
        {
            return this.tlsHandler.DeregisterAsync(context);
        }

        public Task DisconnectAsync(IChannelHandlerContext context)
        {
            return this.tlsHandler.DisconnectAsync(context);
        }

        public void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            if (this.IgnoreException(exception))
            {
                // Close the connection explicitly just in case the transport
                // did not close the connection automatically.
                if (context.Channel.Active)
                {
                    context.CloseAsync();
                }
            }
            else
            {
                tlsHandler.ExceptionCaught(context, exception);
                this.capturedContext.Channel.Pipeline.Remove(this);
            }
        }

        bool IgnoreException(Exception t)
        {
            if (t is ObjectDisposedException && this.closeFuture.Task.IsCompleted)
            {
                return true;
            }
            return false;
        }

        public void Flush(IChannelHandlerContext context)
        {
            this.tlsHandler.Flush(context);
        }

        public void HandlerAdded(IChannelHandlerContext context)
        {
            this.tlsHandler.HandlerAdded(context);
        }

        public void HandlerRemoved(IChannelHandlerContext context)
        {
            this.tlsHandler.HandlerRemoved(context);
        }

        public void Read(IChannelHandlerContext context)
        {
            this.tlsHandler.Read(context);
        }

        public void UserEventTriggered(IChannelHandlerContext context, object evt)
        {
            this.tlsHandler.UserEventTriggered(context, evt);
        }

        public Task WriteAsync(IChannelHandlerContext context, object message)
        {
            return this.tlsHandler.WriteAsync(context, message);
        }

        #endregion
    }

    
}
