using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using PresIt.Data;
using ZXing;
using ZXing.QrCode;
using Image = System.Windows.Controls.Image;

namespace PresIt.Windows {
    public class MainWindowPresenter : INotifyPropertyChanged, IMainWindowPresenter {
        public static readonly EndpointAddress EndPoint =
            new EndpointAddress("http://localhost:9001/PresItService/"); // presit.noip.me

        private readonly string clientId;
        private readonly BitmapImage barcodeImage;

        private ICommand newPresentationCommand;
        private string newPresentationName;
        private IPresItService service;

        public event EventHandler IsAuthenticated;

        public MainWindowPresenter() {
            InitializePresItServiceClient();
            clientId = Guid.NewGuid().ToString();

            var writer = new BarcodeWriter {Format = BarcodeFormat.QR_CODE, Options = new QrCodeEncodingOptions() {Margin = 1}};
            var result = writer.Write(clientId);
            var img = new BitmapImage();

            using (var memory = new MemoryStream()) {
                result.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                img.BeginInit();
                img.StreamSource = memory;
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
            }

            barcodeImage = img;

            new Thread(() => {
                while (!service.IsAuthenticated(clientId)) {
                    Thread.Sleep(1000);
                }
                if (IsAuthenticated != null) IsAuthenticated(this, EventArgs.Empty);
            }).Start();
        }

        public string NewPresentationName {
            get { return newPresentationName; }
            set {
                newPresentationName = value;
                OnPropertyChanged();
            }
        }
        public BitmapImage BarcodeImage {
            get { return barcodeImage; }
        }

        public ICommand NewPresentationCommand {
            get {
                return newPresentationCommand ?? (newPresentationCommand = new RelayCommand(param => {
                    var name = param as string;
                    if (name == null) return;
                    // create new plain presentation
                    Presentation p = service.CreatePresentation(clientId, name);
                    MessageBox.Show(p.Name);
                }, o => !string.IsNullOrEmpty(NewPresentationName)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

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

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}