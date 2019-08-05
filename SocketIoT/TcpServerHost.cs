using System;
using System.Diagnostics.Contracts;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Common.Concurrency;
using DotNetty.Common.Internal.Logging;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Transport.Libuv;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
namespace SocketIoT
{
    class TcpServerHost
    {
        public static void Main2(string[] args)
        {
            AutoResetEvent theCloser = new AutoResetEvent(false);
            Console.WriteLine("Initializing host...");

            int minWorkerThreads;
            int minCompletionPortThreads;
            ThreadPool.GetMinThreads(out minWorkerThreads, out minCompletionPortThreads);
            ThreadPool.SetMinThreads(minWorkerThreads, Math.Max(16, minCompletionPortThreads));
            int threadCount = Environment.ProcessorCount;

            X509Certificate2 tlsServerCertificate = new X509Certificate2(@"protocol-gateway.contoso.com.pfx", "password");

            Task.Factory.StartNew(() =>
            {
                var server = new TcpServerLibuv();
                Console.CancelKeyPress += server.CloseAsync;
                server.StartAsync(tlsServerCertificate, theCloser, threadCount);
                while (true) Thread.Sleep(1000);
            });
            theCloser.WaitOne();
        }

        public static void Main(string[] args)
        {
            int minWorkerThreads;
            int minCompletionPortThreads;
            ThreadPool.GetMinThreads(out minWorkerThreads, out minCompletionPortThreads);
            ThreadPool.SetMinThreads(minWorkerThreads, Math.Max(16, minCompletionPortThreads));

            int threadCount = Environment.ProcessorCount;

            CancellationTokenSource cts = null;
            try
            {
                cts = new CancellationTokenSource();
                X509Certificate2 tlsServerCertificate = new X509Certificate2(@"protocol-gateway.contoso.com.pfx", "password");
                var server = new TcpServer();
                Task.Run(() =>
                {
                    server.StartAsync(tlsServerCertificate, threadCount, cts.Token);
                });
                while (true)
                {
                    string input = Console.ReadLine();
                    if (input != null && input.ToLowerInvariant() == "exit")
                    {
                        break;
                    }
                }
            }
            finally
            {
                cts.Cancel();
            }
        }
    }

    internal class TcpServerLibuv
    {
        IChannel theChannel;
        IEventLoopGroup bossGroup;
        IEventLoopGroup workerGroup;
        AutoResetEvent theCloser;

        public async void StartAsync(X509Certificate2 certificate, AutoResetEvent closer, int threadCount)
        {
            Contract.Requires(certificate != null);
            Contract.Requires(closer != null);
            Contract.Requires(threadCount > 0);

            theCloser = closer;
            Console.WriteLine("Initializing Server...");
            LoggerFactory loggerFactory = new LoggerFactory();
            loggerFactory.AddConsole(Microsoft.Extensions.Logging.LogLevel.Error, false);
            InternalLoggerFactory.DefaultFactory.AddProvider(new ConsoleLoggerProvider((s, level) => true, false));

            var dispatcher = new DispatcherEventLoopGroup();
            bossGroup = dispatcher;
            workerGroup = new WorkerEventLoopGroup(dispatcher, threadCount);

            try
            {
                var bootstrap = new ServerBootstrap();
                bootstrap.Group(bossGroup, workerGroup);
                bootstrap.Channel<TcpServerChannel>();
                bootstrap
                    .Option(ChannelOption.SoBacklog, 100)
                    .Handler(new LoggingHandler("SRV_LSTN"))
                    .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;
                        if (certificate != null)
                        {
                            pipeline.AddLast("tls", TlsHandler.Server(certificate));
                        }
                        pipeline.AddLast("framing-enc", new LengthFieldPrepender(2));
                        pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(ushort.MaxValue, 0, 2, 0, 2));
                        pipeline.AddLast("echo", new EchoServerHandler());
                    }));

                theChannel = await bootstrap.BindAsync(IPAddress.Any, 3382);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }

        public async void CloseAsync(object sender, ConsoleCancelEventArgs e)
        {
            await theChannel.CloseAsync();
            await bossGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
            await workerGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
            theCloser.Set();
        }
    }

    internal class TcpServer
    {
        IChannel theChannel;
        IEventLoopGroup parentEventLoopGroup;
        IEventLoopGroup eventLoopGroup;
        TaskCompletionSource closeCompletionSource;

        public async void StartAsync(X509Certificate2 certificate, int threadCount, CancellationToken cancelToken)
        {
            Contract.Requires(certificate != null);
            Contract.Requires(threadCount > 0);

            try
            {
                parentEventLoopGroup = new MultithreadEventLoopGroup(1);
                eventLoopGroup = new MultithreadEventLoopGroup(threadCount);
                closeCompletionSource = new TaskCompletionSource();

                ServerBootstrap svrBootstrap = new ServerBootstrap()
                    .Group(parentEventLoopGroup, eventLoopGroup)
                    .Option(ChannelOption.SoBacklog, 200)
                    .ChildOption(ChannelOption.Allocator, PooledByteBufferAllocator.Default)
                    .ChildOption(ChannelOption.AutoRead, true)
                    .Channel<TcpServerSocketChannel>()
                    .Handler(new LoggingHandler("SRV_LSTN"))
                    .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                         channel.Pipeline.AddLast(TlsHandler.Server(certificate));
                         channel.Pipeline.AddLast("echo", new EchoServerHandler());
                    }));
                theChannel = await svrBootstrap.BindAsync(IPAddress.Any, 3382);
                cancelToken.Register(closeAsync);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            
        }

        public Task CloseCompletionSource => this.closeCompletionSource.Task;

        private async void closeAsync()
        {
            try
            {
                if (this.theChannel != null)
                {
                    await this.theChannel.CloseAsync();
                }
                if (this.eventLoopGroup != null)
                {
                    await this.eventLoopGroup.ShutdownGracefullyAsync();
                }
            }
            catch(Exception ex)
            {

            }
            finally
            {
                this.closeCompletionSource.TryComplete();
            }


        }

       
    }
}
