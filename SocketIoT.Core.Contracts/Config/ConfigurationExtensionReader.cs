namespace SocketIoT.Core.Common.Config
{
    using Microsoft.Extensions.Configuration;

    public class ConfigurationExtensionReader : IAppConfigReader
    {
        static readonly IConfiguration Config = new ConfigurationBuilder().AddJsonFile("appSettings.json").Build();

        public bool TryGetSetting(string name, out string value)
        {
            IConfigurationSection appsettings = Config.GetSection("AppSettings");
            value = appsettings.GetSection(name).Value;
            if (value == null)
            {
                value = default(string);
                return false;
            }

            return true;
        }
    }
}