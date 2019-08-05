using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;

namespace SocketIoT
{
    internal class EchoServerHandler : ChannelHandlerAdapter
    {
        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            IByteBuffer buffer = message as IByteBuffer;
            if (buffer != null)
            {
                Console.WriteLine("Received from client:" + buffer.ToString(Encoding.UTF8));
            }
            context.WriteAsync(message);
        }

        public override void ChannelReadComplete(IChannelHandlerContext context)
        {
            context.Flush();
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            Console.WriteLine("Exception: " + exception);
            context.CloseAsync();
        }

        public override void ChannelRegistered(IChannelHandlerContext context)
        {
            Console.WriteLine($"CONNECTION: {context.Channel.RemoteAddress}");
            base.ChannelRegistered(context);
        }

        public override void ChannelActive(IChannelHandlerContext context)
        {
            Console.WriteLine($"ACTIVE: {context.Channel.RemoteAddress}");
            base.ChannelActive(context);
        }

        public override void UserEventTriggered(IChannelHandlerContext context, object @event)
        {
            var handshakeCompletionEvent = @event as DotNetty.Handlers.Tls.TlsHandshakeCompletionEvent;
            if (!handshakeCompletionEvent.IsSuccessful)
                context.DeregisterAsync().Wait();
        }



    }
}