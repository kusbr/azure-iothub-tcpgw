
namespace SocketIoT.Bootstrapper
{
    using DotNetty.Handlers.Tls;
    using SocketIoT.Core.Tcp.Config;
    using SocketIoT.Core.Tcp.Credentials;
    using System;
    using System.Diagnostics.Contracts;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    public class TlsBootstrapper : TcpBootstrapperBase
    {

        /// <summary>
        /// Configures the Secure Tcp IoTHub Gateway 
        /// </summary>
        /// <param name="settingsProvider">Configuration settings</param>
        /// <param name="deviceCredentialProvider">Provides the credentials map for the Tcp device using DeviceId</param>
        public TlsBootstrapper(ISettingsProvider settingsProvider, IDeviceCredentialProvider deviceCredentialProvider):
            base(settingsProvider, deviceCredentialProvider)
        {
        }

        /// <summary>
        /// Bootstraps the Tcp IoT Hub Gateway for X509 cert based Tls
        /// </summary>
        /// <param name="certificate2">Server certificate for Tls</param>
        /// <param name="threadCount"># of threads to be used</param>
        /// <param name="cancellationToken">Cancellation flag</param>
        /// <returns></returns>
        public async Task RunAsync(X509Certificate2 certificate2, int threadCount, CancellationToken cancellationToken)
        {
            Contract.Requires(threadCount > 0);

            try
            {
                var serverTlsSettings = new ServerTlsSettings(certificate2, true, true, System.Security.Authentication.SslProtocols.Tls12);
                //var tlsHandler = new Tenancy.X509TlsTenancyHandler(serverTlsSettings);
                var tlsHandler =  TlsHandler.Server(certificate2);

                this.RunAsync(threadCount, cancellationToken, tlsHandler, true);
            }
            catch (Exception ex)
            {
                //BootstrapperEventSource.Log.Error("Failed to start", ex);
                this.CloseAsync();
            }
        }

        private bool ClientCertValidatorCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}
