namespace SocketIoT.Core.Common
{
    using System;
    using System.Collections.Generic;
    using DotNetty.Buffers;

    public interface ITcpMessage : IDisposable
    {
        string Address { get; }

        IByteBuffer Payload { get; }

        string Id { get; }

        IDictionary<string, string> Properties { get; }

        DateTime CreatedTimeUtc { get; }

        uint DeliveryCount { get; }

        ulong SequenceNumber { get; }
    }
}