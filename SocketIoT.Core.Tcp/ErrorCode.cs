namespace SocketIoT.Core.Tcp
{
    public enum ErrorCode
    {
        InvalidErrorCode = 0,

        //BadRequest - 400
        GenericTimeOut = 400002,       
        InvalidOperation = 400003,
        NotSupported = 400004,
        KeepAliveTimedOut = 400005,
        ConnectionTimedOut = 400006,
        ConnectExpected = 400007,
        UnResolvedSendingClient = 400008,
        UnknownPacketType = 400012,

        AuthenticationFailed = 401000,

        ClientClosedRequest = 400499,
        ChannelClosed = 400001,
    }
}
