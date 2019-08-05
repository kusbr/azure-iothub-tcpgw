
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
    using SocketIoT.Core.Tcp.Codec;
    using SocketIoT.Core.Tcp.Config;
    using SocketIoT.Core.Tcp.Credentials;
    using SocketIoT.Core.Tcp.Handlers;
    using SocketIoT.Core.Tcp.Messaging;
    using SocketIoT.IoTHubProvider;
    using SocketIoT.Tenancy;
    using System;
    using System.Diagnostics.Contracts;
    using System.Net;
    using System.Net.Security;
    using System.Resources;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    public class TcpBootstrapperBase
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
        readonly IotHubClientSettings iotHubClientSettings;

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
        /// <param name="firstHandler">Handler to be added as first in the pipeline</param>
        public TcpBootstrapperBase(ISettingsProvider settingsProvider, IDeviceCredentialProvider deviceCredentialProvider)
        {
            Contract.Requires(settingsProvider != null);
            Contract.Requires(deviceCredentialProvider != null);

            this.closeCompletionSource = new TaskCompletionSource();

            this.settingsProvider = settingsProvider;
            this.settings = new Settings(this.settingsProvider);
            this.iotHubClientSettings = new IotHubClientSettings(this.settingsProvider);
            this.authProvider = new SasTokenDeviceIdentityProvider();
            this.credentialProvider = deviceCredentialProvider;
        }

        public Task CloseCompletion => this.closeCompletionSource.Task;

        /// <summary>
        /// Bootstraps the Tcp IoT Hub Gateway
        /// </summary>
        /// <param name="certificate2">Server certificate for Tls</param>
        /// <param name="threadCount"># of threads to be used</param>
        /// <param name="cancellationToken">Cancellation flag</param>
        /// <returns></returns>
        public async Task RunAsync(int threadCount, CancellationToken cancellationToken, ChannelHandlerAdapter firstHandler, bool Tls = true)
        {
            Contract.Requires(threadCount > 0);

            try
            {
                //BootstrapperEventSource.Log.Info("Starting", null);

                //PerformanceCounters.ConnectionsEstablishedTotal.RawValue = 0;
                //PerformanceCounters.ConnectionsCurrent.RawValue = 0;

                this.parentEventLoopGroup = new MultithreadEventLoopGroup(1);
                this.eventLoopGroup = new MultithreadEventLoopGroup(threadCount);

                ServerBootstrap bootstrap = this.SetupBootstrap(firstHandler);

                //BootstrapperEventSource.Log.Info($"Initializing TLS endpoint on port {this.settings.ListeningPort.ToString()} with certificate {this.tlsCertificate.Thumbprint}.", null);
                this.serverChannel = await bootstrap.BindAsync(IPAddress.Any, Tls ? this.settings.SecureListeningPort : this.settings.ListeningPort);

                this.serverChannel.CloseCompletion.LinkOutcome(this.closeCompletionSource);
                cancellationToken.Register(this.CloseAsync);

                //BootstrapperEventSource.Log.Info("Started", null);
            }
            catch (Exception ex)
            {
                //BootstrapperEventSource.Log.Error("Failed to start", ex);
                this.CloseAsync();
            }
        }

        public async Task RunAsync2(int threadCount, CancellationToken cancellationToken, X509Certificate2 cert=null, ServerTlsSettings serverTlsSettings = null, RemoteCertificateValidationCallback rcvb = null)
        {
            Contract.Requires(threadCount > 0);

            try
            {
                //BootstrapperEventSource.Log.Info("Starting", null);

                //PerformanceCounters.ConnectionsEstablishedTotal.RawValue = 0;
                //PerformanceCounters.ConnectionsCurrent.RawValue = 0;

                this.parentEventLoopGroup = new MultithreadEventLoopGroup(1);
                this.eventLoopGroup = new MultithreadEventLoopGroup(threadCount);

                ChannelHandlerAdapter handler = null;
                if (cert != null && serverTlsSettings != null)
                    handler = new TlsHandler(stream => new SslStream(stream, true, rcvb), serverTlsSettings);
                else
                    handler = new DataBasedTenancyHandler();

                ServerBootstrap bootstrap = this.SetupBootstrap(handler);

                //BootstrapperEventSource.Log.Info($"Initializing TLS endpoint on port {this.settings.ListeningPort.ToString()} with certificate {this.tlsCertificate.Thumbprint}.", null);
                this.serverChannel = await bootstrap.BindAsync(IPAddress.Any, rcvb!=null ? this.settings.SecureListeningPort : this.settings.ListeningPort);

                this.serverChannel.CloseCompletion.LinkOutcome(this.closeCompletionSource);
                cancellationToken.Register(this.CloseAsync);

                //BootstrapperEventSource.Log.Info("Started", null);
            }
            catch (Exception ex)
            {
                //BootstrapperEventSource.Log.Error("Failed to start", ex);
                this.CloseAsync();
            }
        }
        #endregion

        #region Protected members
        protected async void CloseAsync()
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
            }
            finally
            {
                this.closeCompletionSource.TryComplete();
            }
        }

        #endregion

        #region Private members

        ServerBootstrap SetupBootstrap(ChannelHandlerAdapter firstHandler)
        {
            Contract.Requires(firstHandler != null);

            return new ServerBootstrap()
                .Group(this.parentEventLoopGroup, this.eventLoopGroup)
                .Option(ChannelOption.SoBacklog, ListenBacklogSize)
                .ChildOption(ChannelOption.Allocator, PooledByteBufferAllocator.Default)
                .Channel<TcpServerSocketChannel>()
                .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
                {
                    int connectionPoolSize = this.settingsProvider.GetIntegerSetting("IotHubClient.ConnectionPoolSize", DefaultConnectionPoolSize);
                    TimeSpan connectionIdleTimeout = this.settingsProvider.GetTimeSpanSetting("IotHubClient.ConnectionIdleTimeout", DefaultConnectionIdleTimeout);
                    string connectionString = this.iotHubClientSettings.IotHubConnectionString;
                    int maxInboundMessageSize = this.settingsProvider.GetIntegerSetting("MaxInboundMessageSize", 256 * 1024);

                    var messagingAddressConverter = new IoTHubProvider.Addressing.MessageAddressConverter();
                    Func<IDeviceIdentity, Task<ITcpIoTHubMessagingServiceClient>> deviceClientFactory
                            = IotHubClient.PreparePoolFactory(connectionString, connectionPoolSize, connectionIdleTimeout, this.iotHubClientSettings);

                    MessagingBridgeFactoryFunc bridgeFactory
                            = async deviceIdentity => new SingleClientMessagingBridge(deviceIdentity, await deviceClientFactory(deviceIdentity));

                    channel.Pipeline.AddLast(firstHandler);
                    channel.Pipeline.AddLast(DeviceTopicDecoder.HandlerName, new DeviceTopicDecoder(this.credentialProvider, messagingAddressConverter.TopicTemplates));
                    channel.Pipeline.AddLast(SocketIoTHubAdapter.HandlerName, new SocketIoTHubAdapter(this.settings, credentialProvider, this.authProvider, bridgeFactory));
                }));
        }
        #endregion
    }
}
