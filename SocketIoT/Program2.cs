using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Security;
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
using SocketIoT.Common;
using SocketIoT.Providers.CloudStorage;

namespace SocketIoT
{
    class Program2
    {
        static IChannel theChannel;
        static IEventLoopGroup bossGroup;
        static IEventLoopGroup workerGroup;

        private static readonly AutoResetEvent _closing = new AutoResetEvent(false);

        protected async static void OnExit(object sender, ConsoleCancelEventArgs args)
        {
            Console.WriteLine("Exit");
            await theChannel.CloseAsync();
            await bossGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
            await workerGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
            _closing.Set();
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("Dotnetty Tcp Server");
            RunTcpServerAsyncLibuv().Wait();
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    Thread.Sleep(1000);
                }
            });
            Console.CancelKeyPress += new ConsoleCancelEventHandler(OnExit);
            _closing.WaitOne();

            //runtcpserverasync().wait();

            //var cts = new cancellationtokensource();
            //var closecompletion = new taskcompletionsource();
            //task.run(() => runtcpserverasynclibuv2(cts.token, closecompletion), cts.token);

            //while (true)
            //{
            //    string input = console.readline();
            //    if (input.tolowerinvariant() == "exit")
            //        break;
            //}
            //cts.cancel();
            //closecompletion.task.wait(timespan.fromseconds(10));
        }

        static async Task RunTcpServerAsyncLibuv()
        {
            Console.WriteLine("Initializing Server...");
            ConsoleLoggerOptions loggeroptions = new ConsoleLoggerOptions() { IncludeScopes = false };
            LoggerFactory loggerFactory = new LoggerFactory();
            loggerFactory.AddConsole(Microsoft.Extensions.Logging.LogLevel.Error, false);
                

            InternalLoggerFactory.DefaultFactory.AddProvider(new ConsoleLoggerProvider((s, level) => true, false));

            var dispatcher = new DispatcherEventLoopGroup();
            bossGroup = dispatcher;
            workerGroup = new WorkerEventLoopGroup(dispatcher);

            X509Certificate2 tlsServerCertificate = new X509Certificate2(@"protocol-gateway.contoso.com.pfx", "password");
            IChannel boundChannel = null;
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
                       if (tlsServerCertificate != null)
                       {
                           var tlsSettings = new ServerTlsSettings(tlsServerCertificate, true, true, System.Security.Authentication.SslProtocols.Tls);
                           var handler = new TlsHandler(stream => new System.Net.Security.SslStream(stream, true, ValidateClientCertificate), tlsSettings);
                           pipeline.AddLast("certTls", handler);
                       }
                       pipeline.AddLast("framing-enc", new LengthFieldPrepender(2));
                       pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(ushort.MaxValue, 0, 2, 0, 2));
                       pipeline.AddLast("echo", new EchoServerHandler());
                   }));
                
                boundChannel = await bootstrap.BindAsync(IPAddress.Any, 3382);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                //boundChannel.CloseAsync().Wait();
                //await bossGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
                //await workerGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
            }

        }

        private static bool ValidateClientCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        static async Task RunTcpServerAsync()
        {
            X509Certificate2 tlsServerCertificate = new X509Certificate2(@"protocol-gateway.contoso.com.pfx", "password");
            var parentEventLoopGroup = new MultithreadEventLoopGroup(1);
            var eventLoopGroup = new MultithreadEventLoopGroup(2);
            ServerBootstrap svrBootstrap = new ServerBootstrap()
                .Group(parentEventLoopGroup, eventLoopGroup)
                .Option(ChannelOption.SoBacklog, 200)
                .ChildOption(ChannelOption.Allocator, PooledByteBufferAllocator.Default)
                .ChildOption(ChannelOption.AutoRead, false)
                .Channel<TcpServerSocketChannel>()
                 .Handler(new LoggingHandler("SRV_LSTN"))
                 .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                {
                    channel.Pipeline.AddLast(TlsHandler.Server(tlsServerCertificate));
                    channel.Pipeline.AddLast("echo", new EchoServerHandler());
                }));
            IChannel theChannel = await svrBootstrap.BindAsync(IPAddress.Any, 3382);
            Console.ReadLine();
            await theChannel.CloseAsync();
        }

        static void BootstrapTcpServer()
        {
            int minWorkerThreads;
            int minCompletionPortThreads;
            ThreadPool.GetMinThreads(out minWorkerThreads, out minCompletionPortThreads);
            ThreadPool.SetMinThreads(minWorkerThreads, Math.Max(16, minCompletionPortThreads));

            int threadCount = Environment.ProcessorCount;

            try
            {
                var cts = new CancellationTokenSource();

                var certificate = new X509Certificate2("protocol-gateway.contoso.com.pfx", "password");
                var settingsProvider = new AppConfigSettingsProvider();
                BlobSessionStatePersistenceProvider blobSessionStateProvider = BlobSessionStatePersistenceProvider.CreateAsync(
                    settingsProvider.GetSetting("BlobSessionStatePersistenceProvider.StorageConnectionString"),
                    settingsProvider.GetSetting("BlobSessionStatePersistenceProvider.StorageContainerName")).Result;


                var bootstrapper = new Bootstrapper(settingsProvider, blobSessionStateProvider);
                Task.Run(() => bootstrapper.RunAsync(certificate, threadCount, cts.Token, ValidateClientCertificate), cts.Token);

                while (true)
                {
                    string input = Console.ReadLine();
                    if (input != null && input.ToLowerInvariant() == "exit")
                    {
                        break;
                    }
                }

                cts.Cancel();
                bootstrapper.CloseCompletion.Wait(TimeSpan.FromSeconds(20));
            }
            catch(Exception ex)
            {
            }
        }

    }
}
