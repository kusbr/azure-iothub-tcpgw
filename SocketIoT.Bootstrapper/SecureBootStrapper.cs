
namespace SocketIoT.Bootstrapper
{

    using DotNetty.Buffers;
    using DotNetty.Common.Concurrency;
    using DotNetty.Common.Utilities;
    using DotNetty.Handlers.Tls;
    using DotNetty.Transport.Bootstrapping;
    using DotNetty.Transport.Channels;
    using DotNetty.Transport.Channels.Sockets;
    using Microsoft.Azure.Devices.ProtocolGateway.Identity;
    using SocketIoT.AzureIoTHubClient;
    using SocketIoT.Core.Common;
    using SocketIoT.Core.Tcp.Codec;
    using SocketIoT.Core.Tcp.Handlers;
    using SocketIoT.Core.Tcp.Messaging;
    using SocketIoT.IoTHubProvider;
    using SocketIoT.IoTHubProvider.Addressing;
    using System;
    using System.Diagnostics.Contracts;
    using System.Net;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    public class SecureBootstrapper
    {
        #region constants
        const int ListenBacklogSize = 200; // connections allowed pending accept
        const int DefaultConnectionPoolSize = 400; // IoT Hub default connection pool size
        #endregion

        #region Member Variables
        static readonly TimeSpan DefaultConnectionIdleTimeout = TimeSpan.FromSeconds(210); // IoT Hub default connection idle timeout

        readonly TaskCompletionSource closeCompletionSource;
        readonly ISettingsProvider settingsProvider;
        readonly IDeviceCredentialProvider credentialProvider;

        readonly Settings settings;
        readonly IDeviceIdentityProvider authProvider;

        X509Certificate2 tlsCertificate;
        IEventLoopGroup parentEventLoopGroup;
        IEventLoopGroup eventLoopGroup;
        IChannel serverChannel;
        #endregion

        #region public members

        /// <summary>
        /// Configures the Tcp IoTHub Gateway
        /// </summary>
        /// <param name="settingsProvider">Configuration settings</param>
        /// <param name="deviceCredentialProvider">Provides the credentials map for the Tcp device using DeviceId</param>
        public SecureBootstrapper(ISettingsProvider settingsProvider, IDeviceCredentialProvider deviceCredentialProvider)
        {
            Contract.Requires(settingsProvider != null);
            Contract.Requires(deviceCredentialProvider != null);

            this.closeCompletionSource = new TaskCompletionSource();

            this.settingsProvider = settingsProvider;
            this.settings = new Settings(this.settingsProvider);
            this.authProvider = new SasTokenDeviceIdentityProvider();
            this.credentialProvider = deviceCredentialProvider;
        }

        public Task CloseCompletion => this.closeCompletionSource.Task;

        /// <summary>
        /// Bootstraps the Tcp IoT Hub Gateway
        /// </summary>
        /// <param name="certificate">Server certificate for Tls</param>
        /// <param name="threadCount"># of threads to be used</param>
        /// <param name="cancellationToken">Cancellation flag</param>
        /// <returns></returns>
        public async Task RunAsync(X509Certificate2 certificate, int threadCount, CancellationToken cancellationToken)
        {
            Contract.Requires(certificate != null);
            Contract.Requires(threadCount > 0);

            try
            {
                //BootstrapperEventSource.Log.Info("Starting", null);

                //PerformanceCounters.ConnectionsEstablishedTotal.RawValue = 0;
                //PerformanceCounters.ConnectionsCurrent.RawValue = 0;

                this.tlsCertificate = certificate;
                this.parentEventLoopGroup = new MultithreadEventLoopGroup(1);
                this.eventLoopGroup = new MultithreadEventLoopGroup(threadCount);

                ServerBootstrap bootstrap = this.SetupBootstrap();

                //BootstrapperEventSource.Log.Info($"Initializing TLS endpoint on port {this.settings.ListeningPort.ToString()} with certificate {this.tlsCertificate.Thumbprint}.", null);
                this.serverChannel = await bootstrap.BindAsync(IPAddress.Any, this.settings.SecureListeningPort);

                this.serverChannel.CloseCompletion.LinkOutcome(this.closeCompletionSource);
                cancellationToken.Register(this.CloseAsync);

                //BootstrapperEventSource.Log.Info("Started", null);
            }
            catch (Exception ex)
            {
                //BootstrapperEventSource.Log.Error("Failed to start", ex);
                Console.WriteLine(ex.Message);
                this.CloseAsync();
            }
        }
        #endregion

        #region private members
        private bool TlsValidatorCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            var cert2 = (certificate as X509Certificate2) ?? new X509Certificate2(certificate);
            return true;
        }

        async void CloseAsync()
        {
            try
            {
                //BootstrapperEventSource.Log.Info("Stopping", null);

                if (this.serverChannel != null)
                {
                    await this.serverChannel.CloseAsync();
                }
                if (this.eventLoopGroup != null)
                {
                    await this.eventLoopGroup.ShutdownGracefullyAsync();
                }

                //BootstrapperEventSource.Log.Info("Stopped", null);
            }
            catch (Exception ex)
            {
                //BootstrapperEventSource.Log.Warning("Failed to stop cleanly", ex);
                Console.WriteLine(ex.Message);

            }
            finally
            {
                this.closeCompletionSource.TryComplete();
            }
        }

        ServerBootstrap SetupBootstrap()
        {
            return new ServerBootstrap()
                .Group(this.parentEventLoopGroup, this.eventLoopGroup)
                .Option(ChannelOption.SoBacklog, ListenBacklogSize)
                .ChildOption(ChannelOption.Allocator, PooledByteBufferAllocator.Default)
                .ChildOption(ChannelOption.AutoRead, true)
                .Channel<TcpServerSocketChannel>()
                .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                {
                    if (this.tlsCertificate != null)
                    {
                        var tlsSettings = new ServerTlsSettings(this.tlsCertificate, true, true, System.Security.Authentication.SslProtocols.Tls12);
                        var handler = new Tenancy.X509TlsTenancyHandler(tlsSettings, this.settingsProvider);
                        channel.Pipeline.AddLast("certTls", handler);
                    }

                    var messagingAddressConverter = new ConfigurableMessageAddressConverter();

                    channel.Pipeline.AddLast(DeviceTopicDecoder.HandlerName, new DeviceTopicDecoder(this.credentialProvider, messagingAddressConverter.TopicTemplates));
                    channel.Pipeline.AddLast(SocketIoTHubAdapter.HandlerName, new SocketIoTHubAdapter(this.settings, credentialProvider, this.authProvider));
                }));
        }
        #endregion
    }
}
