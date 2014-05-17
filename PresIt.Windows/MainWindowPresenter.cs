using System;
using System.ServiceModel;
using System.Windows;
using System.Windows.Input;
using PresIt.Data;

namespace PresIt.Windows {
    public class MainWindowPresenter : IMainWindowPresenter {
        public static readonly EndpointAddress EndPoint =
            new EndpointAddress("http://presit.noip.me:9001/PresItService/");

        private ICommand newPresentationCommand;
        private IPresItService service;


        public MainWindowPresenter() {
            InitializePresItServiceClient();
        }

        public ICommand NewPresentationCommand {
            get {
                return newPresentationCommand ?? (newPresentationCommand = new RelayCommand(param => {
                    if (param is string) {
                        // create new presentation from existing one
                    }
                    else {
                        // create new plain presentation
                        Presentation p = service.CreatePresentation("Hi", "huhu");
                        MessageBox.Show(p.Name);
                    }
                }));
            }
        }

        private void InitializePresItServiceClient() {
            BasicHttpBinding binding = CreateBasicHttp();
            var factory = new ChannelFactory<IPresItService>(binding, EndPoint);
            service = factory.CreateChannel();
        }

        private BasicHttpBinding CreateBasicHttp() {
            var binding = new BasicHttpBinding {
                Name = "basicHttpBinding",
                MaxBufferSize = 2147483647,
                MaxReceivedMessageSize = 2147483647
            };
            var timeout = new TimeSpan(0, 0, 30);
            binding.SendTimeout = timeout;
            binding.OpenTimeout = timeout;
            binding.ReceiveTimeout = timeout;
            return binding;
        }
    }
}