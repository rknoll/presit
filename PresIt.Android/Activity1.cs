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
using Accord.Statistics.Analysis;
using System.Text;
using Android.Hardware;
using System.Collections.Generic;

namespace PresIt.Android {
    [Activity(Label = "PresIt.Android", MainLauncher = true, Icon = "@drawable/icon")]
	public class Activity1 : Activity, ISensorEventListener {
        public static readonly EndpointAddress EndPoint =
            new EndpointAddress("http://192.168.20.2:9001/PresItService/"); //presit.noip.me

        private IPresItService service;
		private string deviceId;


		private SensorManager _sensorManager;
		private static readonly object _syncLock = new object();

		public void OnAccuracyChanged(Sensor sensor, SensorStatus accuracy)
		{
			// We don't want to do anything here.
		}

		public void OnSensorChanged(SensorEvent e)
		{
			lock (_syncLock)
			{
				var x = e.Values [0] * 10;
				var y = e.Values [1] * 10;
				var z = e.Values [2] * 10;

				if (x > 32767) x = 32767;
				if (x < -32768) x = -32768;
				if (y > 32767) x = 32767;
				if (y < -32768) x = -32768;
				if (z > 32767) x = 32767;
				if (z < -32768) x = -32768;

				ReceivedPacket ((short)x, (short)y, (short)z);
			}
		}

        protected override void OnCreate(Bundle bundle) {
			base.OnCreate(bundle);
			//_sensorManager = (SensorManager) GetSystemService(Context.SensorService);

			//RunCalibration ();

            var prefs = PreferenceManager.GetDefaultSharedPreferences(this);

			deviceId = prefs.GetString("deviceId", null);

			if (deviceId == null) {
				deviceId = Guid.NewGuid().ToString();
                var editor = prefs.Edit();
				editor.PutString("deviceId", deviceId);
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
					new System.Threading.Thread(() => service.AuthenticateId(deviceId, result.Text)).Start();
                }
            };

			var button1 = FindViewById<Button>(Resource.Id.button1);
			button1.Click += async (sender, e) => {
				new System.Threading.Thread(() => service.NextSlide(deviceId)).Start();
			};

			var button2 = FindViewById<Button>(Resource.Id.button2);
			button2.Click += async (sender, e) => {
				new System.Threading.Thread(() => service.PreviousSlide(deviceId)).Start();
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

			//new System.Threading.Thread (DeviceThread).Start(socket);
        }

		protected override void OnResume()
		{
			base.OnResume();
			//_sensorManager.RegisterListener(this, _sensorManager.GetDefaultSensor(SensorType.Accelerometer), SensorDelay.Ui);
		}

		protected override void OnPause()
		{
			base.OnPause();
			//_sensorManager.UnregisterListener(this);
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

		void RunCalibration() {
			double[][] inputs = 
			{
				// Class 0 
				new double[] {  4,  1 }, 
				new double[] {  2,  4 },
				new double[] {  2,  3 },
				new double[] {  3,  6 },
				new double[] {  4,  4 },

				// Class 1 
				new double[] {  9, 10 },
				new double[] {  6,  8 },
				new double[] {  9,  5 },
				new double[] {  8,  7 },
				new double[] { 10,  8 }
			};

			int[] output = 
			{
				0, 0, 0, 0, 0, // The first five are from class 0 
				1, 1, 1, 1, 1  // The last five are from class 1
			};

			var lda = new LinearDiscriminantAnalysis(inputs, output);

			lda.Compute(); // Compute the analysis 


			// Now we can project the data into KDA space: 
			double[][] projection = lda.Transform(inputs);

			// Or perform classification using: 
			int[] results = lda.Classify(inputs);

			var sb = new StringBuilder();
			foreach(var r in results) {
				if (sb.Length != 0) sb.Append(", ");
				sb.Append(string.Format("{0}", r));
			}
			Console.WriteLine(sb.ToString());

			for(int i=0; i<2; ++i) {
				lastValuesX.Enqueue(0);
			}

			lastEvent = 0;
		}

		private Queue<int> lastValuesX = new Queue<int> ();
		private int lastEvent;
		private DateTime lastEventTime;

		void ReceivedPacket (short x, short y, short z) {



			if (lastEventTime != null && lastEvent == 1) {
				lastEvent = 2;
				Console.WriteLine (DateTime.Now.Subtract (lastEventTime).TotalMilliseconds);
			}
			lastEvent = lastEvent == 0 ? 1 : lastEvent;
			lastEventTime = DateTime.Now;
			/*RunOnUiThread (() => {
				Toast.MakeText (this, string.Format ("X: {0}, Y: {1}, Z: {2}", x, y, z), ToastLength.Short).Show ();
			});

			service.NextSlide (deviceId);*/

			/*
			if (Math.Abs (x) > 100) {
				if (DateTime.Now.Subtract (lastEventTime).TotalMilliseconds > 100) {
					if (x > 0) {
						Console.WriteLine ("Left");

					} else {
						Console.WriteLine ("Right");
					}
				}
				lastEventTime = DateTime.Now;
			}
			*/
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