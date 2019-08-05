namespace SocketIoT.IoTHubProvider
{
    using SocketIoT.Core.Common;
    using System;

    public class IotHubClientSettings
    {
        const string SettingPrefix = "IotHubClient.";
        const string IotHubConnectionStringSetting = SettingPrefix + "ConnectionString"; // connection string to IoT Hub. Credentials can be overriden by device specific credentials coming from auth provider
        const string MaxPendingInboundMessagesSetting = SettingPrefix + "MaxPendingInboundMessages"; // number of messages after which driver stops reading from network. Reading from network will resume once one of the accepted messages is completely processed.
        const string MaxPendingOutboundMessagesSetting = SettingPrefix + "MaxPendingOutboundMessages"; // number of messages in flight after which driver stops receiving messages from IoT Hub queue. Receiving will resume once one of the messages is completely processed.
        const string MaxOutboundRetransmissionCountSetting = SettingPrefix + "MaxOutboundRetransmissionCount";
        const string ServicePropertyPrefixSetting = SettingPrefix + "ServicePropertyPrefix";

        const int MaxPendingInboundMessagesDefaultValue = 16;
        const int MaxPendingOutboundMessagesDefaultValue = 1;
        const int NoMaxOutboundRetransmissionCountValue = -1;

        public IotHubClientSettings(ISettingsProvider settingsProvider, string tenantId="")
        {
            int inboundMessages;
            if (!settingsProvider.TryGetIntegerSetting(MaxPendingInboundMessagesSetting, out inboundMessages) || inboundMessages <= 0)
            {
                inboundMessages = MaxPendingInboundMessagesDefaultValue;
            }
            this.MaxPendingInboundMessages = Math.Min(inboundMessages, ushort.MaxValue); // reflects packet id domain per MQTT spec.

            int outboundMessages;
            if (!settingsProvider.TryGetIntegerSetting(MaxPendingOutboundMessagesSetting, out outboundMessages) || outboundMessages <= 0)
            {
                outboundMessages = MaxPendingOutboundMessagesDefaultValue;
            }
            this.MaxPendingOutboundMessages = Math.Min(outboundMessages, ushort.MaxValue >> 2); // limited due to separation of packet id domains for QoS 1 and 2.

            string connectionString = "";
            if (!string.IsNullOrEmpty(tenantId))
            {
                connectionString = settingsProvider.GetSetting(string.Format("{0}.{1}", tenantId, IotHubConnectionStringSetting));
            }
            else
            {
                connectionString = settingsProvider.GetSetting(IotHubConnectionStringSetting);

            }
            if (connectionString.IndexOf("DeviceId=", StringComparison.OrdinalIgnoreCase) == -1)
            {
                connectionString += ";DeviceId=stub";
            }
            this.IotHubConnectionString = connectionString;

            int retransmissionCount;
            if (!settingsProvider.TryGetIntegerSetting(MaxOutboundRetransmissionCountSetting, out retransmissionCount)
                || (retransmissionCount < 0))
            {
                retransmissionCount = NoMaxOutboundRetransmissionCountValue;
            }

            this.ServicePropertyPrefix = settingsProvider.GetSetting(ServicePropertyPrefixSetting, string.Empty);
        }

        public int MaxPendingInboundMessages { get; private set; }

        public int MaxPendingOutboundMessages { get; private set; }

        public string IotHubConnectionString { get; private set; }

        public string ServicePropertyPrefix { get; private set; }
    }
}