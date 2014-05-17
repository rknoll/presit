using System;
using System.ServiceModel;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Preferences;
using Android.Widget;
using PresIt.Data;

namespace PresIt.Android {
    [Activity(Label = "PresIt.Android", MainLauncher = true, Icon = "@drawable/icon")]
    public class Activity1 : Activity {
        public static readonly EndpointAddress EndPoint =
            new EndpointAddress("http://192.168.20.2:9001/PresItService/"); //presit.noip.me

        private IPresItService service;
        private string uuid;

        protected override void OnCreate(Bundle bundle) {
            base.OnCreate(bundle);

            var prefs = PreferenceManager.GetDefaultSharedPreferences(this);

            uuid = prefs.GetString("uuid", null);

            if (uuid == null) {
                uuid = Guid.NewGuid().ToString();
                var editor = prefs.Edit();
                editor.PutString("uuid", uuid);
                editor.Commit();
            }

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            InitializePresItServiceClient();

            // Get our button from the layout resource,
            // and attach an event to it
            var button = FindViewById<Button>(Resource.Id.MyButton);

            /*button.Click += delegate {
                Presentation result = service.CreatePresentation("Richard", "First Presentation");
                button.Text = result.Name;
            };*/

            button.Click += async (sender, e) =>
            {

                //NOTE: On Android you MUST pass a Context into the Constructor!
                var scanner = new ZXing.Mobile.MobileBarcodeScanner(this);
                var result = await scanner.Scan();

                if (result != null) {
                    service.AuthenticateId(uuid, result.Text);
                }
            };
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