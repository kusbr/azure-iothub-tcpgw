
namespace SocketIoT.Bootstrapper
{
    using SocketIoT.Core.Tcp.Config;
    using SocketIoT.Core.Tcp.Credentials;
    using SocketIoT.Tenancy;
    using System;
    using System.Diagnostics.Contracts;
    using System.Threading;
    using System.Threading.Tasks;

    public class NonTlsBootstrapper : TcpBootstrapperBase
    {

        /// <summary>
        /// Configures the Tcp IoTHub Gateway with NonTls support
        /// </summary>
        /// <param name="settingsProvider">Configuration settings</param>
        /// <param name="deviceCredentialProvider">Provides the credentials map for the Tcp device using DeviceId</param>
        public NonTlsBootstrapper(ISettingsProvider settingsProvider, IDeviceCredentialProvider deviceCredentialProvider):
            base(settingsProvider, deviceCredentialProvider)
        {
        }

        /// <summary>
        /// Bootstraps the Tcp IoT Hub Gateway
        /// </summary>
        /// <param name="certificate2">Server certificate for Tls</param>
        /// <param name="threadCount"># of threads to be used</param>
        /// <param name="cancellationToken">Cancellation flag</param>
        /// <returns></returns>
        public async Task RunAsync(int threadCount, CancellationToken cancellationToken)
        {
            Contract.Requires(threadCount > 0);

            try
            {
                var firstHandler = new DataBasedTenancyHandler();
                await this.RunAsync(threadCount, cancellationToken, firstHandler, false);
            }
            catch (Exception ex)
            {
                //BootstrapperEventSource.Log.Error("Failed to start", ex);
                this.CloseAsync();
            }
        }
       
    }
}
