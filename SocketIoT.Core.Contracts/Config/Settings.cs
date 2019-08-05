using System;

namespace SocketIoT.Core.Common
{
    public class Settings 
    {
        const string MaxKeepAliveTimeoutSetting = "MaxKeepAliveTimeout";
        const string ServicePropertyPrefixSetting = "ServicePropertyPrefix";
        const string ListeningPortSetting = "ListeningPort";
        const string SecureListeningPortSetting = "SecureListeningPort";

        readonly int Default_Port = 11000;
        readonly int Default_SecurePort = 11001;

        public Settings(ISettingsProvider settingsProvider)
        {
            
            TimeSpan timeout;
            this.MaxKeepAliveTimeout = settingsProvider.TryGetTimeSpanSetting(MaxKeepAliveTimeoutSetting, out timeout)
                ? timeout
                : (TimeSpan?)null;

            this.ServicePropertyPrefix = settingsProvider.GetSetting(ServicePropertyPrefixSetting, string.Empty);

            int port;
            this.ListeningPort = settingsProvider.TryGetIntegerSetting(ListeningPortSetting, out port) ? port : Default_Port;
            this.SecureListeningPort = settingsProvider.TryGetIntegerSetting(SecureListeningPortSetting, out port) ? port : Default_SecurePort;
        }

        public TimeSpan? MaxKeepAliveTimeout { get; private set; }

        public string ServicePropertyPrefix { get; private set; }

        public int ListeningPort { get; private set; }

        public int SecureListeningPort { get; private set; }

    }
}
