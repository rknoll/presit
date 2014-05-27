using System;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using Android.App;
using Android.Bluetooth;
using Android.OS;
using Android.Preferences;
using Android.Views;
using Android.Widget;
using PresIt.Android.GestureRecognition;
using PresIt.Android.GestureRecognition.Classifier;
using PresIt.Android.GestureRecognition.Sensors;
using PresIt.Data;
using ZXing.Mobile;

namespace PresIt.Android {
    [Activity(Label = "PresIt.Android", MainLauncher = true, Icon = "@drawable/icon")]
    public class Activity1 : Activity, IGestureRecognitionListener {
        private const string ServerAddress = "presit.noip.me";
        private static readonly EndpointAddress serverEndpoint = new EndpointAddress("http://" + ServerAddress + "/PresItService/");

        private string clientId;
        private IPresItService service;
        private GestureRecognitionService recognitionService;
        private Vibrator vibrator;

        private string GetClientId() {
            var prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            var id = prefs.GetString("clientId", null);
            if (id != null) return id;
            id = Guid.NewGuid().ToString();
            var editor = prefs.Edit();
            editor.PutString("clientId", id);
            editor.Commit();
            return id;
        }


        protected override void OnCreate(Bundle bundle) {
            base.OnCreate(bundle);
            clientId = GetClientId();


            var sensorSource = new PhoneSensorSource(this);
            //var sensorSource = new BluetoothSensorSource(this);


            recognitionService = new GestureRecognitionService(sensorSource);
            recognitionService.RegisterListener(this);
            recognitionService.StartClassificationMode();
            vibrator = (Vibrator) GetSystemService(VibratorService);
            SetContentView(Resource.Layout.Main);

            InitializePresItServiceClient();

            var authenticateButton = FindViewById<Button>(Resource.Id.AuthenticateButton);
            authenticateButton.Click += async (sender, e) => {
                var scanner = new MobileBarcodeScanner(this);
                var result = await scanner.Scan();
                if (result != null) new Thread(() => service.AuthenticateId(clientId, result.Text)).Start();
            };

            var nextSlideButton = FindViewById<Button>(Resource.Id.NextSlideButton);
            nextSlideButton.Click += (sender, e) => NextSlide();

            var previousSlideButton = FindViewById<Button>(Resource.Id.PreviousSlideButton);
            previousSlideButton.Click += (sender, e) => PreviousSlide();

            var trainingButton = FindViewById<Button>(Resource.Id.TrainingButton);
            trainingButton.Click += (sender, e) => {
                if (trainingButton.Text == "Start Training Left") {
                    trainingButton.Text = "Start Training Right";
                    recognitionService.StartLearnMode("left");
                } else if (trainingButton.Text == "Start Training Right") {
                    trainingButton.Text = "Stop Training";
                    recognitionService.StopLearnMode();
                    recognitionService.StartLearnMode("right");
                } else {
                    trainingButton.Text = "Start Training Left";
                    recognitionService.StopLearnMode();
                }
            };
            /*
            IEnumerable<PresentationPreview> presentationPreviews = service.GetPresentationPreviews(clientId);
            PresentationPreview firstPresentation = presentationPreviews == null
                ? null
                : presentationPreviews.FirstOrDefault();

            if (firstPresentation != null && firstPresentation.FirstSlide != null &&
                firstPresentation.FirstSlide.ImageData != null) {
                var image = FindViewById<ImageView>(Resource.Id.image);
                Bitmap bmp = BitmapFactory.DecodeByteArray(firstPresentation.FirstSlide.ImageData, 0,
                    firstPresentation.FirstSlide.ImageData.Length);
                image.SetImageBitmap(bmp);
            }
            */
        }

        private void NextSlide() {
            new Thread(() => service.NextSlide(clientId)).Start();
        }

        private void PreviousSlide() {
            new Thread(() => service.PreviousSlide(clientId)).Start();
        }

        private void InitializePresItServiceClient() {
            var binding = CreateBasicHttp();
            var factory = new ChannelFactory<IPresItService>(binding, serverEndpoint);
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

        public override bool OnKeyUp(Keycode keyCode, KeyEvent e) {
            switch (keyCode) {
                case Keycode.VolumeDown:
                    PreviousSlide();
                    return true;
                case Keycode.VolumeUp:
                    NextSlide();
                    return true;
            }
            return base.OnKeyUp(keyCode, e);
        }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e) {
            if (keyCode == Keycode.VolumeDown || keyCode == Keycode.VolumeUp) return true;
            return base.OnKeyDown(keyCode, e);
        }

        public void OnGestureRecognized(Distribution distribution) {
            if (distribution.BestDistance > 8) return;
            if (distribution.BestMatch == "left") {
                PreviousSlide();
            } else if (distribution.BestMatch == "right") {
                NextSlide();
            }
        }

        public void OnGestureLearned(string gestureName) {
            RunOnUiThread(() => vibrator.Vibrate(100));
        }

        public void OnTrainingSetDeleted() {
            
        }
    }
}