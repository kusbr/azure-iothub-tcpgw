namespace SocketIoT.Core.Common.Config
{
    using Microsoft.Extensions.Configuration;
    using System.IO;

    class ConfigManagerReader : IAppConfigReader
    {
        IConfiguration config; 
        public ConfigManagerReader()
        {
            System.Console.WriteLine(Directory.GetCurrentDirectory());
            config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appSettings.json", true, true)
                .Build();
        }

        public bool TryGetSetting(string name, out string value)
        {
            value = config[name];
            if (value == null) return false;
            return true;
        }
    }
}
