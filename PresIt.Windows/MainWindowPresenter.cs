using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using PresIt.Data;
using ZXing;
using ZXing.QrCode;

namespace PresIt.Windows {
    public class MainWindowPresenter : INotifyPropertyChanged, IMainWindowPresenter {
        public static readonly EndpointAddress EndPoint =
            new EndpointAddress("http://presit.noip.me/PresItService/"); // presit.noip.me

        private readonly BitmapImage barcodeImage;
        private string clientId;

        private ICommand newPresentationCommand;
        private ICommand deletePresentationCommand;

        private string newPresentationName;
        private IPresItService service;
        private bool isAuthenticated;
        private Presentation currentPresentation;

        public MainWindowPresenter() {
            InitializePresItServiceClient();
            var sessionId = Guid.NewGuid().ToString();

            var writer = new BarcodeWriter {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions {Margin = 1}
            };

            var result = writer.Write(sessionId);
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
                        clientId = service.IsAuthenticated(sessionId);
                        if (clientId == null) continue;
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
        public event EventHandler<int> GotPresentationSlidesCount;
        public event EventHandler GotPresentationSlide;
        public event EventHandler CancelStartPresentation;

        private Thread commandThread;

        public void SavePresentation() {
            new Thread(() => {
                if (currentPresentation == null) {
                    if (CancelStartPresentation != null) CancelStartPresentation(this, null);
                    return;
                }
                var slides = currentPresentation.Slides == null ? new List<Slide>() : currentPresentation.Slides.ToList();
                if (GotPresentationSlidesCount != null) GotPresentationSlidesCount(this, slides.Count);
                service.UpdateSlidesCount(clientId, currentPresentation.Id, slides.Count);
                for (var i = 0; i < slides.Count; ++i) {
                    service.UpdateSlide(clientId, currentPresentation.Id, slides[i]);
                    if (GotPresentationSlide != null) GotPresentationSlide(this, null);
                }
                if (PresentationSaved != null) PresentationSaved(this, null);
            }).Start();
        }

        public void GetPresentations() {
            new Thread(() => {
                if (PresentationList != null) {
                    var previews = FetchPresentationPreviews();
                    if (previews == null) {
                        if (CancelStartPresentation != null) CancelStartPresentation(this, null);
                        return;
                    }
                    if (PresentationList != null) PresentationList(this, previews);
                } else {
                    if (CancelStartPresentation != null) CancelStartPresentation(this, null);
                }
            }).Start();
        }

        public void StartPresentation(SlidePreview presentationPreview) {
            new Thread(() => {
                if (ShowPresentation != null) {
                    var p = FetchPresentation(presentationPreview);
                    if (p == null) {
                        if (CancelStartPresentation != null) CancelStartPresentation(this, null);
                        return;
                    }
                    ShowPresentation(this, p);
                    commandThread = new Thread(RequestNextCommand);
                    commandThread.Start();
                } else {
                    if (CancelStartPresentation != null) CancelStartPresentation(this, null);
                }
            }).Start();
        }

        private Presentation FetchPresentation(SlidePreview presentationPreview) {
            var p = new Presentation();
            p.Id = presentationPreview.PresentationId;
            p.Name = presentationPreview.SlideText;
            p.Owner = clientId;
            var slides = new List<Slide>();

            var cnt = service.GetPresentationSlidesCount(clientId, presentationPreview.PresentationId);
            if (cnt < 0) return null;
            if (GotPresentationSlidesCount != null) GotPresentationSlidesCount(this, cnt);
            for (var i = 0; i < cnt; ++i) {
                var slide = service.GetPresentationSlide(clientId, presentationPreview.PresentationId, i);
                if (slide == null) return null;
                if (GotPresentationSlide != null) GotPresentationSlide(this, null);
                slides.Add(slide);
            }
            p.Slides = slides;
            return p;
        }
        
        private IEnumerable<PresentationPreview> FetchPresentationPreviews() {
            var previews = new List<PresentationPreview>();

            var cnt = service.GetPresentationCount(clientId);
            if (cnt < 0) return null;
            if (GotPresentationSlidesCount != null) GotPresentationSlidesCount(this, cnt);
            for (var i = 0; i < cnt; ++i) {
                var preview = service.GetPresentationPreview(clientId, i);
                if (preview == null) return null;
                if (GotPresentationSlide != null) GotPresentationSlide(this, null);
                previews.Add(preview);
            }
            return previews;
        }

        private void RequestNextCommand() {
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
                } catch (Exception e) {
                    if (e is ThreadAbortException) break;
                }
            }
        }

        public void StopPresentation() {
            commandThread.Abort();
        }

        public void ChangePresentation(SlidePreview presentationPreview) {
            new Thread(() => {
                if (EditPresentation != null) {
                    var p = FetchPresentation(presentationPreview);
                    if (p == null) return;
                    currentPresentation = p;
                    CommandManager.InvalidateRequerySuggested();
                    EditPresentation(this, currentPresentation);
                } else {
                    if (CancelStartPresentation != null) CancelStartPresentation(this, null);
                }
            }).Start();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void InitializePresItServiceClient() {
            var binding = CreateBasicHttp();
            var factory = new ChannelFactory<IPresItService>(binding, EndPoint);
            service = factory.CreateChannel();
        }

        private Binding CreateBasicHttp() {
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