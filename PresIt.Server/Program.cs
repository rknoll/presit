using System.ServiceModel;
using System.Threading;
using PresIt.Service;

namespace PresIt.Server {
    internal static class Program {
        /// <summary>
        ///     The main entry point for the server application.
        /// </summary>
        private static void Main() {
            // create the service
            var host = new ServiceHost(typeof (PresItService));
            host.Open(); // start the service
            var close = new AutoResetEvent(false);
            host.Closed += (sender, args) => close.Set();
            // wait until it is closed
            close.WaitOne();
        }
    }
}