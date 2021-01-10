using DotNetty.Common;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.ProtocolGateway.Identity;
using Microsoft.Azure.Devices.ProtocolGateway.Messaging;
using SocketIoT.Core.Common;
using SocketIoT.Core.Tcp.Messaging;
using SocketIoT.Core.Tcp.Packets;
using SocketIoT.Core.Tcp.Tenancy;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;

namespace SocketIoT.Core.Tcp.Handlers
{
    public class SocketIoTHubAdapter : ChannelHandlerAdapter
    {
        #region public strings
        public static readonly string HandlerName = "SocketIoTHubAdapter";

        public const string OperationScopeExceptionDataKey = "PG.SocketIoTHubAdapter.Scope.Operation";
        public const string ConnectionScopeExceptionDataKey = "PG.SocketIoTHubAdapter.Scope.Connection";

        const string ConnectProcessingScope = "Connect";
        #endregion

        #region local static members
        static readonly Action<object> CheckKeepAliveCallback = CheckKeepAlive;
        #endregion

        #region local members
        IChannelHandlerContext capturedContext;
        StateFlags stateFlags;
        DateTime lastClientActivityTime;
        Queue<Packet> connectPendingQueue;
        DeviceDataPacket dataPacketWithConnect;
        IDeviceIdentity identity;
        TimeSpan keepAliveTimeout;
        ITcpIoTHubMessagingBridge messagingBridge;
        
        readonly Settings settings;
        readonly IDeviceIdentityProvider authProvider;
        MessagingBridgeFactoryFunc messagingBridgeFactory;
        readonly IList<string> messages;

        string ChannelId => this.capturedContext.Channel.Id.ToString();
        bool ConnectedToService => this.messagingBridge != null;

        #endregion

        #region public methods and constructors
        public SocketIoTHubAdapter
            (
                Settings settings, 
                IDeviceCredentialProvider credentialProvider,
                IDeviceIdentityProvider authProvider
            )
        {
            Contract.Requires(settings != null);
            Contract.Requires(authProvider != null);
            Contract.Requires(credentialProvider != null);

            this.settings = settings;
            this.authProvider = authProvider;
            this.messages = new List<string>();
        }

        #endregion

        #region IChannelHandler  Overrides
        public override void ChannelActive(IChannelHandlerContext context)
        {
            this.capturedContext = context;
            this.stateFlags = StateFlags.NotConnected;
            base.ChannelActive(context);
        }
        
        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var tenancyContext = context.GetAttribute<AbstractTenancyContext>(AttributeKey<AbstractTenancyContext>.ValueOf(AbstractTenancyContext.TENANCY_CONTEXT_KEY)).Get();
            this.messagingBridgeFactory = tenancyContext.IotBridgeFactory;

            var packets = message as IDictionary<PacketType, Packet>;

            if (packets == null || (packets!= null && packets.Count <= 0))
            {
                Console.WriteLine(($"No messages (`{typeof(Packet).FullName}` ) received"));
                return;
            }

            var connectPacket = packets[PacketType.CONNECT] as ConnectPacket;
            var dataPacket = packets[PacketType.D2C] as DeviceDataPacket;

            this.lastClientActivityTime = DateTime.Now;
            if (this.IsInState(StateFlags.Connected))   //Already Connected, process DeviceDataPacket
            {
                this.ProcessPacket(context, dataPacket);
            }
            else if (this.IsInState(StateFlags.ProcessingConnect))   //Connect processing in progress, queue newer connect requests
            {
                //TODO Implement queue/ priority queue based on supported cases
                Queue<Packet> queue = this.connectPendingQueue ?? (this.connectPendingQueue = new Queue<Packet>(4));
                queue.Enqueue(dataPacket);
            }
            else
            {
                //Not Connected and Not processing a connect - Use connect packet to create connection to IoTHub
                this.dataPacketWithConnect = dataPacket;
                this.ProcessPacket(context, connectPacket);
            }

            context.WriteAsync(message);
        }

        public override void ChannelUnregistered(IChannelHandlerContext context)
        {
            DisconnectDevice().Wait();
            base.ChannelUnregistered(context);
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            if (exception is System.Net.Sockets.SocketException)
            {
                switch ((exception as System.Net.Sockets.SocketException).SocketErrorCode)
                {
                    case System.Net.Sockets.SocketError.ConnectionReset:
                        string deviceId = "";
                        if (this.messagingBridge != null)
                        {
                            if (this.messagingBridge.TryResolveClient("Events", out var iotClient))
                            {
                                deviceId = iotClient.DeviceId;
                            }
                        }
                        Console.WriteLine($"Connection reset by device {deviceId}");
                        break;
                }
            }
            else
            {
                Console.WriteLine("ERROR:" + exception.Message);
                Console.WriteLine(exception.StackTrace);
            }
            DisconnectDevice().Wait();
            Shutdown(context, exception);
            context.CloseAsync();
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            base.ChannelInactive(context);
        }
        #endregion

        #region Process packet to IoT Hub
        void ProcessPacket(IChannelHandlerContext context, Packet packet)
        {
            if (this.IsInState(StateFlags.Closed))
            {
                Console.WriteLine($"Message was received after channel closure: {packet}, identity: {this.identity}", this.ChannelId);
                return;
            }

            switch (packet.PacketType)
            {
                case PacketType.REGISTER:
                    break;

                case PacketType.DPS_REGISTER:
                    break;

                case PacketType.CONNECT:    //Connect to IoTHub and store persist session state
                    this.Connect(context, packet as ConnectPacket);
                    break;

                case PacketType.D2C:
                    this.SendD2CMessage(packet as DeviceDataPacket);
                    break;

                case PacketType.C2D:
                    break;

                case PacketType.DEVCE_TWIN_UPDATE:
                    break;

                case PacketType.FILE_UPLOAD:
                    break;

                case PacketType.HEART_BEAT:
                    break;

                case PacketType.DISCONNECT:
                    break;
            }
        }
        #endregion

        #region Lifecylce management events - Connect, CompleteConnect, KeepAlive, Disconnect, Data Send, Shutdown

        #region Connect
        async void Connect(IChannelHandlerContext context, ConnectPacket packet)
        {
            try
            {
                this.stateFlags = StateFlags.ProcessingConnect;

                this.identity = await this.authProvider.GetAsync(
                    packet.DeviceId, packet.Username, packet.Password, this.capturedContext.Channel.RemoteAddress);

                if (!this.identity.IsAuthenticated)
                {
                    Console.WriteLine("ClientNotAuthenticated", $"Client ID: {packet.DeviceId}; Username: {packet.Username}", this.ChannelId);
                    ShutdownOnError(context, ConnectProcessingScope, new SocketIoTGatewayException(ErrorCode.AuthenticationFailed, "Authentication failed."));
                    return;
                }

                Console.WriteLine($"Device {this.identity.Id} Authenticated");

                this.messagingBridge = await this.messagingBridgeFactory(this.identity);

                this.keepAliveTimeout = this.DeriveKeepAliveTimeout(context, packet);

                this.CompleteConnect(context);

            }
            catch (Exception e)
            {
                SocketIoTHubAdapter.ShutdownOnError(context, ConnectProcessingScope, e);
            }

        }

        /// <summary>
        ///     Finalizes initialization based on CONNECT packet: dispatches keep-alive timer and releases messages buffered before
        ///     the CONNECT processing was finalized.
        /// </summary>
        /// <param name="context"><see cref="IChannelHandlerContext" /> instance.</param>
        void CompleteConnect(IChannelHandlerContext context)
        {
            Console.WriteLine($"Connection established:{this.identity.ToString()} channelId:  {this.ChannelId}");

            if (this.keepAliveTimeout > TimeSpan.Zero)
            {
                CheckKeepAlive(context);
            }

            this.stateFlags = StateFlags.Connected;

            //this.messagingBridge.BindMessagingChannel(this);
           
            if (this.connectPendingQueue != null)
            {
                while (this.connectPendingQueue.Count > 0)
                {
                    Packet packet = this.connectPendingQueue.Dequeue();
                    this.ProcessPacket(context, packet);
                }
                this.connectPendingQueue = null; // release unnecessary queue
            }

            //process any data sent with connect
            if (this.dataPacketWithConnect?.PacketType == PacketType.D2C && this.dataPacketWithConnect.Payload?.ReadableBytes > 0)
            {
                ProcessPacket(context, dataPacketWithConnect);
            }
        }

        static void CheckKeepAlive(object ctx)
        {
            var context = (IChannelHandlerContext)ctx;
            var self = (SocketIoTHubAdapter)context.Handler;
            TimeSpan elapsedSinceLastActive = DateTime.UtcNow - self.lastClientActivityTime;
            if (elapsedSinceLastActive > self.keepAliveTimeout)
            {
                ShutdownOnError(context, string.Empty, new SocketIoTGatewayException(ErrorCode.KeepAliveTimedOut, "Keep Alive timed out."));
                return;
            }

            context.Channel.EventLoop.ScheduleAsync(CheckKeepAliveCallback, context, self.keepAliveTimeout - elapsedSinceLastActive);
        }

        TimeSpan DeriveKeepAliveTimeout(IChannelHandlerContext context, ConnectPacket packet)
        {
            TimeSpan timeout = TimeSpan.FromSeconds(packet.KeepAliveSeconds * 1.5);
            TimeSpan? maxTimeout = this.settings.MaxKeepAliveTimeout;
            if (maxTimeout.HasValue && (timeout > maxTimeout.Value || timeout == TimeSpan.Zero))
            {
                Console.WriteLine($"Requested Keep Alive timeout is longer than the max allowed. Limiting to max value of {maxTimeout.Value}.", null, this.ChannelId);
                return maxTimeout.Value;
            }

            return timeout;
        }

        #endregion

        #region Disconnect

        async Task DisconnectDevice()
        {
            if (this.messagingBridge != null)
            {
                if (this.messagingBridge.TryResolveClient("Events", out var iotClient))
                {
                    Console.WriteLine($"Closing device {iotClient.DeviceId} connection to Azure IoT Hub...");
                    await iotClient?.CloseAsync();
                }
            }
        }

        #endregion

        #region Data 
        async void SendD2CMessage(DeviceDataPacket dataPacket)
        {
            Contract.Requires(this.identity != null);

            if (identity?.Id != dataPacket.DeviceId)
            {
                if (this.stateFlags == StateFlags.ProcessingConnect || this.stateFlags == StateFlags.Connected)
                {
                    this.stateFlags = StateFlags.InvalidConfiguration;
                    this.Shutdown(this.capturedContext, new SocketIoTGatewayException(ErrorCode.UnResolvedSendingClient, "Invalid device identity"));
                    return;
                }
                
            }


            if (this.ConnectedToService)
            {
                PreciseTimeSpan startedTimestamp = PreciseTimeSpan.FromStart;
                
                    IMessage message = null;
                try
                {
                    ITcpIoTHubMessagingServiceClient sendingClient = null;
                    if (this.messagingBridge.TryResolveClient("Events", out sendingClient))
                    {
                        message = sendingClient.CreateMessage(dataPacket.EventTopicAddress, dataPacket.Payload);
                        message.Properties[this.settings.ServicePropertyPrefix + "MessageType"] = dataPacket.PacketType.ToString();
                        await sendingClient.SendAsync(message);
                        message = null;
                    }
                    else
                    {
                        throw new SocketIoTGatewayException(ErrorCode.UnResolvedSendingClient, $"Could not resolve a sending client based on topic name `Events`.");
                    }
                }
                finally
                {
                    message?.Dispose();
                }
                
            }
            else
            {
                dataPacket.Release();
            }
        }

        #endregion

        /// <summary>
        ///     Initiates closure of both channel and hub connection.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="scope">Scope where error has occurred.</param>
        /// <param name="error">Exception describing the error leading to closure.</param>
        static void ShutdownOnError(IChannelHandlerContext context, string scope, Exception error)
        {
            Contract.Requires(error != null);

            if (error != null && !string.IsNullOrEmpty(scope))
            {
                error.Data[OperationScopeExceptionDataKey] = scope;
                error.Data[ConnectionScopeExceptionDataKey] = context.Channel.Id.ToString();
            }

            var self = (SocketIoTHubAdapter)context.Handler;
            if (!self.IsInState(StateFlags.Closed))
            {
                //PerformanceCounters.ConnectionFailedOperationalPerSecond.Increment();
                self.Shutdown(context, error);
            }
        }

        /// <summary>
        ///     Closes channel
        /// </summary>
        async void Shutdown(IChannelHandlerContext context, Exception cause)
        {
            if (this.IsInState(StateFlags.Closed))
            {
                return;
            }

            try
            {
                this.stateFlags |= StateFlags.Closed; // "or" not to interfere with ongoing logic which has to honor Closed state when it's right time to do (case by case)

                // only decrement connection current counter if the state had connected state in this session 
                if (this.IsInState(StateFlags.Connected))
                {
                    //PerformanceCounters.ConnectionsCurrent.Decrement();
                }

                Queue<Packet> connectQueue = this.connectPendingQueue;
                if (connectQueue != null)
                {
                    while (connectQueue.Count > 0)
                    {
                        Packet packet = connectQueue.Dequeue();
                        ReferenceCountUtil.Release(packet);
                    }
                }

                //await this.CloseServiceConnection(context, cause, will);
                await context.CloseAsync();
            }
            catch (Exception ex)
            {
                //LOG
                Console.WriteLine("Error occurred while shutting down the channel.", ex, this.ChannelId);
            }
        }

        #endregion

        #region Helpers
        bool IsInState(StateFlags stateFlagsToCheck) => (this.stateFlags & stateFlagsToCheck) == stateFlagsToCheck;

        bool ResetState(StateFlags stateFlagsToReset)
        {
            StateFlags flags = this.stateFlags;
            this.stateFlags = flags & ~stateFlagsToReset;
            return (flags & stateFlagsToReset) != 0;
        }

        [Flags]
        enum StateFlags
        {
            NotConnected = 1,
            ProcessingConnect = 1 << 1,
            Connected = 1 << 2,
            Closed = 1 << 3,
            ReadThrottled = 1 << 4,
            InvalidConfiguration = 1 << 5
        }
        #endregion
    }
}
