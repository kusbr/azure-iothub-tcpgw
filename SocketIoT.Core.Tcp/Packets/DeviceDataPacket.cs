namespace SocketIoT.Core.Tcp.Packets
{

    using DotNetty.Buffers;
    using DotNetty.Common;

    public sealed class DeviceDataPacket : Packet, IByteBufferHolder
    {
        public DeviceDataPacket(string deviceId, string address, IByteBuffer data) :  base(deviceId)
        {
            this.Payload = data;
            this.EventTopicAddress = address;
        }

        public override PacketType PacketType => PacketType.D2C;

        public string EventTopicAddress { get; set; }

        public IByteBuffer Payload { get;  }

        public int ReferenceCount => this.Payload.ReferenceCount;

        public IReferenceCounted Retain()
        {
            this.Payload.Retain();
            return this;
        }

        public IReferenceCounted Retain(int increment)
        {
            this.Payload.Retain(increment);
            return this;
        }

        public IReferenceCounted Touch()
        {
            this.Payload.Touch();
            return this;
        }

        public IReferenceCounted Touch(object hint)
        {
            this.Payload.Touch(hint);
            return this;
        }

        public bool Release() => this.Payload.Release();

        public bool Release(int decrement) => this.Payload.Release(decrement);

        IByteBuffer IByteBufferHolder.Content => this.Payload;

        public IByteBufferHolder Copy() => this.Replace(this.Payload.Copy());

        public IByteBufferHolder Replace(IByteBuffer content)
        {
            var result = new DeviceDataPacket(this.DeviceId, this.EventTopicAddress, content);
            return result;
        }

        IByteBufferHolder IByteBufferHolder.Duplicate() => this.Replace(this.Payload.Duplicate());

        public IByteBufferHolder RetainedDuplicate() => this.Replace(this.Payload.RetainedDuplicate());
    }
}
