namespace SocketIoT.Core.Tcp
{
    using SocketIoT.Core.Tcp;
    using System;

    public class SocketIoTGatewayException : Exception
    {
        public bool IsTransient { get; }

        public string TrackingId { get; private set; }

        public ErrorCode ErrorCode { get; private set; }

        public SocketIoTGatewayException(ErrorCode errorCode, string message)
            : this(errorCode, message, false)
        {
        }

        public SocketIoTGatewayException(ErrorCode errorCode, string message, string trackingId)
            : this(errorCode, message, false, trackingId)
        {
        }

        public SocketIoTGatewayException(ErrorCode errorCode, string message, bool isTransient)
            : this(errorCode, message, isTransient, string.Empty)
        {
        }

        public SocketIoTGatewayException(ErrorCode errorCode, string message, bool isTransient, string trackingId)
            : base(message)
        {
            this.IsTransient = isTransient;
            this.TrackingId = trackingId;
            this.ErrorCode = errorCode;
        }
    }
}
