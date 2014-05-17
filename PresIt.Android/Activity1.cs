using System;
using System.ServiceModel;
using Android.App;
using Android.OS;
using Android.Widget;
using PresIt.Data;

namespace PresIt.Android {
    [Activity(Label = "PresIt.Android", MainLauncher = true, Icon = "@drawable/icon")]
    public class Activity1 : Activity {
        public static readonly EndpointAddress EndPoint =
            new EndpointAddress("http://presit.noip.me:9001/PresItService/");

        private IPresItService service;

        protected override void OnCreate(Bundle bundle) {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            InitializePresItServiceClient();

            // Get our button from the layout resource,
            // and attach an event to it
            var button = FindViewById<Button>(Resource.Id.MyButton);

            button.Click += delegate {
                Presentation result = service.CreatePresentation("Richard", "First Presentation");
                button.Text = result.Name;
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