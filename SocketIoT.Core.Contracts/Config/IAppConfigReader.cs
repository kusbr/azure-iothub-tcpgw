namespace SocketIoT.Core.Common
{
    public interface IAppConfigReader
    {
        bool TryGetSetting(string name, out string value);
    }
}
