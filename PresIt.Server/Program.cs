using System.ServiceModel;
using System.Threading;
using PresIt.Service;

namespace PresIt.Server {
    internal static class Program {
        private static ServiceHost host;
        private static AutoResetEvent close;

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        private static void Main() {
            if (host != null) host.Close();
            host = new ServiceHost(typeof (PresItService));
            host.Open();
            close = new AutoResetEvent(false);
            host.Closed += (sender, args) => close.Set();
            close.WaitOne();
        }
    }
}