using SocketIoT.Bootstrapper;
using SocketIoT.Core.Common;
using SocketIoT.Core.Common.Config;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIoT
{
    class SocketServerHost
    {
        public static void Main(string[] args)
        {
            BootstrapTcpServers();
        }

        static void BootstrapTcpServers()
        {
            int minWorkerThreads;
            int minCompletionPortThreads;
            ThreadPool.GetMinThreads(out minWorkerThreads, out minCompletionPortThreads);
            ThreadPool.SetMinThreads(minWorkerThreads, Math.Max(16, minCompletionPortThreads));

            int procCount = Environment.ProcessorCount;
            int tlsThreadCount = procCount / 2;
            int nonTlsThreadCount = (procCount - tlsThreadCount) > 0 ? (procCount - tlsThreadCount) : 1;

            //var eventListener = new ObservableEventListener();
            //eventListener.LogToConsole();
            //eventListener.EnableEvents(BootstrapperEventSource.Log, EventLevel.Verbose);
            //eventListener.EnableEvents(DefaultEventSource.Log, EventLevel.Verbose);

            CancellationTokenSource cts1 = null;
            CancellationTokenSource cts2 = null;
            SecureBootstrapper tlsBootstrapper = null;
            Bootstrapper.Bootstrapper nonTlsBootstrapper = null;
            try
            {
                cts1 = new CancellationTokenSource();
                cts2 = new CancellationTokenSource();
                IDeviceCredentialProvider deviceSasTokenProvider1 = new InMemoryDeviceCredentialProvider();
                IDeviceCredentialProvider deviceSasTokenProvider2 = new InMemoryDeviceCredentialProvider();
                var settingsProvider1 = new AppConfigSettingsProvider();
                var settingsProvider2 = new AppConfigSettingsProvider();

                nonTlsBootstrapper = new Bootstrapper.Bootstrapper(settingsProvider1, deviceSasTokenProvider1);
                tlsBootstrapper = new SecureBootstrapper(settingsProvider2, deviceSasTokenProvider2);

                Task.Run(() => nonTlsBootstrapper.RunAsync(nonTlsThreadCount, cts1.Token), cts1.Token);
                Task.Run(() => tlsBootstrapper.RunAsync(new X509Certificate2("protocol-gateway.contoso.com.pfx", "password"), tlsThreadCount, cts2.Token), cts2.Token);


                while (true)
                {
                    string input = Console.ReadLine();
                    if (input != null && input.ToLowerInvariant() == "exit")
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                cts1.Cancel();
                cts2.Cancel();
                tlsBootstrapper.CloseCompletion.Wait(TimeSpan.FromSeconds(20));
                nonTlsBootstrapper.CloseCompletion.Wait(TimeSpan.FromSeconds(20));
            }
        }

        public static bool ValidateClientCertificate(object sender, X509Certificate certificate, X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            X509Certificate2 cert2 = certificate as X509Certificate2 ?? new X509Certificate2(certificate);
            Console.WriteLine($"TLS.ClientCertificate.Validation: Issuer: {cert2.Issuer} Sub: {cert2.Subject} Thumbprint: {cert2.Thumbprint}");
            //if (cert2.Issuer != "knowncustomer.domain")
            //{
            //    Console.WriteLine("TLS.ClientCertificate.Validation FAILED");
            //    return false;
            //}
            return true;
        }

    }
}
