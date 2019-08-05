namespace SocketIoT.Core.Common.Config
{
    public class AppConfigSettingsProvider : ISettingsProvider
    {
        IAppConfigReader configStrategy;

        public AppConfigSettingsProvider()
        {
#if NETSTANDARD1_3
            this.configStrategy = new ConfigurationExtensionReader();
#else
            this.configStrategy = new ConfigManagerReader();
#endif
        }
        public bool TryGetSetting(string name, out string value)
        {
            return configStrategy.TryGetSetting(name, out value);
        }
    }
}