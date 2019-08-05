using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using SocketIoT.Core.Tcp.Packets;
using System;
using System.Collections.Generic;

namespace SocketIoT.Core.Tcp.Codec
{
    public sealed class StreamPacketDecoder : ReplayingDecoder<StreamPacketDecoder.DecoderState>
    {
        readonly int maxMessageSize;

        public StreamPacketDecoder(int maxMessageSize) : base(DecoderState.Ready)
        {
            this.maxMessageSize = maxMessageSize;
        }

        protected override void Decode(IChannelHandlerContext context, IByteBuffer input, List<object> output)
        {
            try
            {
                switch (this.State)
                {
                    case DecoderState.Ready:
                        Packet packet;
                        if (!this.TryDecodePacket(input, context, out packet))
                        {
                            this.RequestReplay();
                            return;
                        }
                        output.Add(packet);
                        this.Checkpoint();
                        break;

                    case DecoderState.Failed:
                        // read out data until connection is closed
                        input.SkipBytes(input.ReadableBytes);
                        return;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (DecoderException)
            {
                input.SkipBytes(input.ReadableBytes);
                this.Checkpoint(DecoderState.Failed);
                throw;
            }
        }

        private bool TryDecodePacket(IByteBuffer buffer, IChannelHandlerContext context, out Packet packet)
        {
            if (!buffer.IsReadable(2)) // packet consists of at least 2 bytes
            {
                packet = null;
                return false;
            }

            int signature = buffer.ReadByte();  //TODO: Determine your own packet signatures 

            int remainingLength;
            if (!this.TryDecodeRemainingLength(buffer, out remainingLength) || !buffer.IsReadable(remainingLength))
            {
                packet = null;
                return false;
            }

            packet = this.DecodePacketInternal(buffer, signature, ref remainingLength, context);

            if (remainingLength > 0)
            {
                throw new DecoderException($"Declared remaining length is bigger than packet data size by {remainingLength}.");
            }

            return true;
        }

        Packet DecodePacketInternal(IByteBuffer buffer, int packetSignature, ref int remainingLength, IChannelHandlerContext context)
        {
            switch (packetSignature) // strict match checks for valid message type + correct values in flags part
            {
                case PacketSignatures.RegisterDevice:
                    return null;

                case PacketSignatures.DPSRegisterDevice:
                    return null;

                case PacketSignatures.ConnectDevice:
                    var connectPacket = new ConnectPacket(
                        "tcp-sas",
                        @"theEdgeHub.azure-devices.net/tcp-sas",
                        "SharedAccessSignature sr=theEdgeHub.azure-devices.net%2Fdevices%2Fm1plc&sig=wwg2lwZ9YgVNJzNFlObDN5ZpnMRZnGpGVtvpxIFEboM%3D&se=1571826576",
                        120);
                    return connectPacket;

                case PacketSignatures.DeviceDataSend:
                    return null;

                case PacketSignatures.DeviceHeartBeat:
                    return null;

                case PacketSignatures.CloudDataSend:
                    return null;

                case PacketSignatures.DeviceTwinUpdate:
                    return null;

                case PacketSignatures.DeviceFileUpload:
                    return null;

                case PacketSignatures.DisconnectDevice:
                    return null;

                default:
                    throw new DecoderException($"First packet byte value of `{packetSignature}` is invalid.");
            }
        }

        bool TryDecodeRemainingLength(IByteBuffer buffer, out int value)
        {
            int readable = buffer.ReadableBytes;

            int result = 0;
            int multiplier = 1;
            byte digit;
            int read = 0;
            do
            {
                if (readable < read + 1)
                {
                    value = default(int);
                    return false;
                }
                digit = buffer.ReadByte();
                result += (digit & 0x7f) * multiplier;
                multiplier <<= 7;
                read++;
            }
            while ((digit & 0x80) != 0 && read < 4);

            if (read == 4 && (digit & 0x80) != 0)
            {
                throw new DecoderException("Remaining length exceeds 4 bytes in length");
            }

            int completeMessageSize = result + 1 + read;
            if (completeMessageSize > this.maxMessageSize)
            {
                throw new DecoderException("Message is too big: " + completeMessageSize);
            }

            value = result;
            return true;
        }

        public enum DecoderState
        {
            Ready,
            Decoding, 
            Failed  
        }
            
    }
}
