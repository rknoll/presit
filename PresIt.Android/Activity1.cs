using System;
using System.ServiceModel;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Preferences;
using Android.Widget;
using PresIt.Data;
using System.Threading;
using Android.Bluetooth;
using System.Linq;
using Java.Lang;

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

			//InitializePresItServiceClient();

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
                    new System.Threading.Thread(() => service.AuthenticateId(uuid, result.Text)).Start();
                }
            };

			var adapter = BluetoothAdapter.DefaultAdapter;

			if (adapter == null) {
				AlertDialog alertMessage = new AlertDialog.Builder(this).Create();
				alertMessage.SetTitle("PresIt");
				alertMessage.SetMessage("No Bluetooth found..");
				alertMessage.Show();
				return;
			}

			if (!adapter.IsEnabled) {
				AlertDialog alertMessage = new AlertDialog.Builder(this).Create();
				alertMessage.SetTitle("PresIt");
				alertMessage.SetMessage("Bluetooth disabled..");
				alertMessage.Show();
				return;
			}

			var device = adapter.BondedDevices.Where (d => d.Name.ToLower ().Contains ("squad")).FirstOrDefault ();

			if (device == null) {
				AlertDialog alertMessage = new AlertDialog.Builder(this).Create();
				alertMessage.SetTitle("PresIt");
				alertMessage.SetMessage("Bluetooth Device not found..");
				alertMessage.Show();
				return;
			}

			var socket = device.CreateRfcommSocketToServiceRecord(Java.Util.UUID.FromString("00001101-0000-1000-8000-00805f9b34fb"));
			socket.Connect ();

			new System.Threading.Thread (DeviceThread).Start(socket);
        }

		private void DeviceThread(object s) {
			var socket = s as BluetoothSocket;
			if (socket == null) return;

			short x, y, z;

			while (true) {
				var b = socket.InputStream.ReadByte ();
				if (b == 2) {
					b = socket.InputStream.ReadByte (); if (b < 0) continue; x = (short)(((short)b) << 8);
					b = socket.InputStream.ReadByte (); if (b < 0) continue; x |= ((short)b);
					b = socket.InputStream.ReadByte (); if (b < 0) continue; y = (short)(((short)b) << 8);
					b = socket.InputStream.ReadByte (); if (b < 0) continue; y |= ((short)b);
					b = socket.InputStream.ReadByte (); if (b < 0) continue; z = (short)(((short)b) << 8);
					b = socket.InputStream.ReadByte (); if (b < 0) continue; z |= ((short)b);
					b = socket.InputStream.ReadByte ();
					if (b == 3) {
						ReceivedPacket (x, y, z);
					}
				}


			}

		}

		void ReceivedPacket (short x, short y, short z) {
			RunOnUiThread (() => {
				Toast.MakeText (this, string.Format ("X: {0}, Y: {1}, Z: {2}", x, y, z), ToastLength.Short).Show ();
			});
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