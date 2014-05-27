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

    /// <summary>
    /// Core Logic for PresIt
    /// </summary>
    public class MainWindowPresenter : INotifyPropertyChanged, IMainWindowPresenter {

        // Server Remote Address (for debugging use: http://localhost:9001/PresItService/)
        public static readonly EndpointAddress EndPoint =
            new EndpointAddress("http://presit.noip.me/PresItService/"); // presit.noip.me

        private readonly BitmapImage barcodeImage;
        private string clientId;

        private ICommand newPresentationCommand;
        private ICommand deletePresentationCommand;

        private string newPresentationName;
        private IPresItService service;
        private Presentation currentPresentation;
        private readonly List<ISlidesImporter> importers; 

        public MainWindowPresenter() {
            // init server connection
            InitializePresItServiceClient();

            // create random session ID to be authenticated by user
            var sessionId = Guid.NewGuid().ToString();

            // create and show QR code of session ID
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

            // Connect to server and wait until we get authenticated
            new Thread(() => {
                while (true) {
                    try {
                        clientId = service.IsAuthenticated(sessionId);
                        if (clientId == null) continue;
                        if (IsAuthenticated != null) IsAuthenticated(this, EventArgs.Empty);
                        break;
                    } catch (TimeoutException) {
                    }
                }
            }).Start();

            // create slide importers
            importers = new List<ISlidesImporter> {
                new ImageImporter(),
                new PowerPointImporter()
            };
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
        public string DroppedFileName { get; set; }

        private Thread commandThread;

        /// <summary>
        /// Save a Presentation and display a progressbar
        /// </summary>
        public void SavePresentation() {
            new Thread(() => {
                if (currentPresentation == null) {
                    if (CancelStartPresentation != null) CancelStartPresentation(this, null);
                    return;
                }
                var slides = currentPresentation.Slides == null ? new List<Slide>() : currentPresentation.Slides.ToList();
                if (GotPresentationSlidesCount != null) GotPresentationSlidesCount(this, slides.Count);
                service.UpdateSlidesCount(clientId, currentPresentation.Id, slides.Count);
                foreach (var slide in slides) {
                    service.UpdateSlide(clientId, currentPresentation.Id, slide);
                    if (GotPresentationSlide != null) GotPresentationSlide(this, null);
                }
                if (PresentationSaved != null) PresentationSaved(this, null);
            }).Start();
        }

        /// <summary>
        /// Fetch all Presentations and show a progressbar
        /// </summary>
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

        /// <summary>
        /// Start a Presentation and start a Command Receive Thread
        /// </summary>
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

        /// <summary>
        /// Get All Slides of a Presentation
        /// </summary>
        private Presentation FetchPresentation(SlidePreview presentationPreview) {
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

            return new Presentation {
                Id = presentationPreview.PresentationId,
                Name = presentationPreview.SlideText,
                Owner = clientId,
                Slides = slides
            };
        }
        
        /// <summary>
        /// Get all Previews of the Presentations
        /// </summary>
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

        /// <summary>
        /// Get the next Command from the Server and Execute it
        /// </summary>
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

        /// <summary>
        /// Stop presentation and command receive thread
        /// </summary>
        public void StopPresentation() {
            commandThread.Abort();
        }

        /// <summary>
        /// Edit a Presentation, so fetch and display as a first Step
        /// </summary>
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

        /// <summary>
        /// Import Slides to a specific Position in a Presentation
        /// </summary>
        public void ImportSlides(int slideIndex) {
            var importedSlides = new List<Slide>();

            new Thread(() => {
                var fileName = DroppedFileName;
                DroppedFileName = null;
                if (currentPresentation == null) {
                    if (CancelStartPresentation != null) CancelStartPresentation(this, null);
                    return;
                }

                int slideNumber = 1;

                if (currentPresentation.Slides != null) {
                    foreach (var slide in currentPresentation.Slides) {
                        slideNumber++;
                        importedSlides.Add(slide);
                    }
                }

                // if the user dropped a file, import it with an importer
                if (fileName != null) {
                    foreach (var importer in importers) {
                        if (!importer.CanHandle(fileName)) continue;
                        foreach (var slideData in importer.Convert(fileName)) {
                            if (GotPresentationSlidesCount != null) GotPresentationSlidesCount(this, slideData.TotalSlides);
                            if (GotPresentationSlide != null) GotPresentationSlide(this, null);
                            importedSlides.Insert(slideIndex != -1 ? (slideIndex-1) : slideNumber-1, new Slide {
                                ImageData = slideData.CurrentSlideData,
                                SlideNumber = slideIndex != -1 ? slideIndex++ : slideNumber++
                            });
                            if (slideIndex != -1) slideNumber++;
                        }
                        break;
                    }
                }

                if (slideIndex != -1) {
                    for (slideIndex--; slideIndex < slideNumber - 1; ++slideIndex) {
                        importedSlides[slideIndex].SlideNumber = slideIndex + 1;
                    }
                }
                
                currentPresentation.Slides = importedSlides;

                if (EditPresentation != null) EditPresentation(this, currentPresentation);
            }).Start();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // Server Connection Setup

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