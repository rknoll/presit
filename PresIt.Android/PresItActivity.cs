using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using Android.App;
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
    [Activity(Label = "PresIt", MainLauncher = true, Icon = "@drawable/icon")]
    public class PresItActivity : Activity, IGestureRecognitionListener {

        // address of WCF Service (for debugging: localhost:9001)
        private const string ServerAddress = "presit.noip.me";
        private static readonly EndpointAddress serverEndpoint = new EndpointAddress("http://" + ServerAddress + "/PresItService/");

        // our unique ID which is stored on the device, the main login key
        private string clientId;

        // the remote service
        private IPresItService service;

        // vibrating feedback for gesture recognition
        private Vibrator vibrator;

        private GestureRecognitionService internalRecognitionService;
        private GestureRecognitionService externalRecognitionService;

        // get stored client ID or create a new one and save it
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

        // entry point
        protected override void OnCreate(Bundle bundle) {
            base.OnCreate(bundle);
            clientId = GetClientId();

            // gesture recognition
            internalRecognitionService = new GestureRecognitionService(new PhoneSensorSource(this));
            externalRecognitionService = new GestureRecognitionService(new BluetoothSensorSource(this));

            // register callbacks
            internalRecognitionService.RegisterListener(this);
            externalRecognitionService.RegisterListener(this);

            // start detecting gestures
            internalRecognitionService.StartClassificationMode();
            externalRecognitionService.StartClassificationMode();

            // keep light on and screen unlocked
            Window.AddFlags(WindowManagerFlags.KeepScreenOn);

            // get a vibrator service for feedback
            vibrator = (Vibrator) GetSystemService(VibratorService);

            // init screen layout
            SetContentView(Resource.Layout.Main);

            // init server connection
            InitializePresItServiceClient();

            // register button click events
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
                var text = trainingButton.Text;
                SetTraining(internalRecognitionService, ref text);
                trainingButton.Text = text;
            };

            var externalTrainingButton = FindViewById<Button>(Resource.Id.ExternalTrainingButton);
            externalTrainingButton.Click += (sender, e) => {
                var text = externalTrainingButton.Text;
                SetTraining(externalRecognitionService, ref text);
                externalTrainingButton.Text = text;
            };
        }

        private void SetTraining(GestureRecognitionService s, ref string buttonText) {
            switch (buttonText) {
                case "Start Training Left":
                    buttonText = "Start Training Right";
                    s.StartLearnMode("left");
                    break;
                case "Start Training Right":
                    buttonText = "Start Training Pause";
                    s.StopLearnMode();
                    s.StartLearnMode("right");
                    break;
                case "Start Training Pause":
                    buttonText = "Stop Training";
                    s.StopLearnMode();
                    s.StartLearnMode("pause");
                    break;
                default:
                    buttonText = "Start Training Left";
                    s.StopLearnMode();
                    break;
            }
        }

        // switch to next slide
        private void NextSlide() {
            new Thread(() => service.SendCommand(clientId, CommandType.NextSlide)).Start();
        }

        // switch to previous slide
        private void PreviousSlide() {
            new Thread(() => service.SendCommand(clientId, CommandType.PreviousSlide)).Start();
        }
        
        // pause / unpause presentation
        private void SwitchPause() {
            new Thread(() => service.SendCommand(clientId, CommandType.Pause)).Start();
        }

        // server connection initialization
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

        // hardware button callbacks
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

        // gesture recognition callbacks
        public void OnGestureRecognized(GestureRecognitionService source, Distribution distribution) {
            if (source == internalRecognitionService) {
                if (distribution.BestDistance > 8) return;
                RunOnUiThread(() => vibrator.Vibrate(100));
            } else if (source == externalRecognitionService) {
                if (distribution.BestDistance > 8) return;
                RunOnUiThread(() => Toast.MakeText(this, "Gesture " + distribution.BestMatch + " recognized", ToastLength.Short).Show());
            }

            if (distribution.BestMatch == "left") {
                PreviousSlide();
            } else if (distribution.BestMatch == "right") {
                NextSlide();
            } else if (distribution.BestMatch == "pause") {
                SwitchPause();
            }
        }

        public void OnGestureLearned(GestureRecognitionService source, string gestureName) {
            if (source == internalRecognitionService) {
                RunOnUiThread(() => vibrator.Vibrate(100));
            } else if (source == externalRecognitionService) {
                RunOnUiThread(() => Toast.MakeText(this, "Gesture " + gestureName + " learned", ToastLength.Short).Show());
            }
        }

        public void OnTrainingSetDeleted(GestureRecognitionService source) {
        }
    }
}