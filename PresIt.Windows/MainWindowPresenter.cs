using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Threading;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using PresIt.Data;
using ZXing;
using ZXing.QrCode;

namespace PresIt.Windows {
    public class MainWindowPresenter : INotifyPropertyChanged, IMainWindowPresenter {
        public static readonly EndpointAddress EndPoint =
            new EndpointAddress("http://192.168.20.2:9001/PresItService/"); // presit.noip.me

        private readonly BitmapImage barcodeImage;
        private readonly string clientId;

        private ICommand newPresentationCommand;
        private ICommand getPresentationsCommand;
        private ICommand savePresentationCommand;
        private ICommand deletePresentationCommand;
        private string newPresentationName;
        private IPresItService service;
        private bool isAuthenticated;
        private Presentation currentPresentation;

        public MainWindowPresenter() {
            InitializePresItServiceClient();
            clientId = Guid.NewGuid().ToString();

            var writer = new BarcodeWriter {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions {Margin = 1}
            };
            Bitmap result = writer.Write(clientId);
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
                while (true) {
                    try {
                        if (!service.IsAuthenticated(clientId)) continue;
                        isAuthenticated = true;
                        if (IsAuthenticated != null) IsAuthenticated(this, EventArgs.Empty);
                        break;
                    } catch (TimeoutException) {
                    }
                }
            }).Start();
        }

        public string NewPresentationName {
            get { return newPresentationName; }
            set {
                newPresentationName = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
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
                    if (EditPresentation != null) {
                        currentPresentation = service.CreatePresentation(clientId, name);
                        CommandManager.InvalidateRequerySuggested();
                        EditPresentation(this, currentPresentation);
                    }
                }, o => !string.IsNullOrEmpty(NewPresentationName)));
            }
        }
        
        public ICommand GetPresentationsCommand {
            get {
                return getPresentationsCommand ?? (getPresentationsCommand = new RelayCommand(param => {
                    if (PresentationList != null) PresentationList(this, service.GetPresentationPreviews(clientId));
                }, o => isAuthenticated));
            }
        }
        
        public ICommand SavePresentationCommand {
            get {
                return savePresentationCommand ?? (savePresentationCommand = new RelayCommand(param => {
                    service.UpdateSlides(clientId, currentPresentation);
                    if (PresentationSaved != null) PresentationSaved(this, null);
                }, o => currentPresentation != null));
            }
        }

        public ICommand DeletePresentationCommand {
            get {
                return deletePresentationCommand ?? (deletePresentationCommand = new RelayCommand(param => {
                    service.DeletePresentation(clientId, currentPresentation.Id);
                    if (PresentationDeleted != null) PresentationDeleted(this, null);
                }, o => currentPresentation != null));
            }
        }

        public event EventHandler IsAuthenticated;
        public event EventHandler<Presentation> EditPresentation;
        public event EventHandler<Presentation> ShowPresentation;
        public event EventHandler<IEnumerable<PresentationPreview>> PresentationList;
        public event EventHandler PresentationSaved;
        public event EventHandler PresentationDeleted;
        public event EventHandler NextSlide;
        public event EventHandler PreviousSlide;

        private Thread commandThread;

        public void StartPresentation(string presentationId) {
            if (ShowPresentation != null) {
                ShowPresentation(this, service.GetPresentation(clientId, presentationId));
                commandThread = new Thread(RequestNextCommand);
                commandThread.Start();
            }
        }

        private void RequestNextCommand() {
            try {
                while(true) {
                    try {
                        var command = service.GetNextCommand(clientId);
                        switch (command) {
                            case CommandType.Error:
                                Thread.Sleep(1000);
                                break;
                            case CommandType.NextSlide:
                                NextSlide(this, null);
                                break;
                            case CommandType.PreviousSlide:
                                PreviousSlide(this, null);
                                break;
                        }
                    } catch(TimeoutException) {}
                }
            } catch (ThreadAbortException) {}
        }

        public void StopPresentation() {
            commandThread.Abort();
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