using System.Linq;
using System.Threading;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Java.IO;
using Java.Util;
using Console = System.Console;

namespace PresIt.Android.GestureRecognition.Sensors {
    public class BluetoothSensorSource : ISensorSource {
        private readonly Context context;
        private ISensorListener listener;
        private Thread thread;

        public BluetoothSensorSource(Context context) {
            this.context = context;
        }

        public void SetSensorListener(ISensorListener l) {
            listener = l;
            if (listener != null) {
                Start();
            } else {
                Stop();
            }
        }

        private void Start() {
            BluetoothAdapter adapter = BluetoothAdapter.DefaultAdapter;

            if (adapter == null) {
                AlertDialog alertMessage = new AlertDialog.Builder(context).Create();
                alertMessage.SetTitle("PresIt");
                alertMessage.SetMessage("No Bluetooth found..");
                alertMessage.Show();
                return;
            }

            if (!adapter.IsEnabled) {
                AlertDialog alertMessage = new AlertDialog.Builder(context).Create();
                alertMessage.SetTitle("PresIt");
                alertMessage.SetMessage("Bluetooth disabled..");
                alertMessage.Show();
                return;
            }

            BluetoothDevice device = adapter.BondedDevices.FirstOrDefault(d => d.Name.ToLower().Contains("squad"));

            if (device == null) {
                AlertDialog alertMessage = new AlertDialog.Builder(context).Create();
                alertMessage.SetTitle("PresIt");
                alertMessage.SetMessage("Bluetooth Device not found..");
                alertMessage.Show();
                return;
            }

            BluetoothSocket socket =
                device.CreateRfcommSocketToServiceRecord(UUID.FromString("00001101-0000-1000-8000-00805f9b34fb"));
            
            thread = new Thread(DeviceThread);
            thread.Start(socket);
        }

        private void Stop() {
            if (thread != null) {
                try {
                    thread.Abort();
                }
                catch (ThreadAbortException) {
                }
            }
        }

        private void DeviceThread(object s) {
            try {
                var socket = s as BluetoothSocket;
                if (socket == null) {
                    return;
                }

                do {
                    try {
                        Console.WriteLine("Connecting..");
                        socket.Connect();
                    }
                    catch (Java.IO.IOException) {
                    }
                } while (!socket.IsConnected);

                Console.WriteLine("Connected.");
                bool receivingData = false;

                while (true) {
                    int b = socket.InputStream.ReadByte();
                    if (!receivingData) {
                        receivingData = true;
                        Console.WriteLine("Receiving Data.");
                    }
                    if (b == 2) {
                        b = socket.InputStream.ReadByte();
                        if (b < 0) {
                            continue;
                        }
                        var x = (short) b;
                        b = socket.InputStream.ReadByte();
                        if (b < 0) {
                            continue;
                        }
                        x |= (short) (((short)b) << 8);
                        b = socket.InputStream.ReadByte();
                        if (b < 0) {
                            continue;
                        }
                        var y = (short) b;
                        b = socket.InputStream.ReadByte();
                        if (b < 0) {
                            continue;
                        }
                        y |= (short) (((short)b) << 8);
                        b = socket.InputStream.ReadByte();
                        if (b < 0) {
                            continue;
                        }
                        var z = (short) b;
                        b = socket.InputStream.ReadByte();
                        if (b < 0) {
                            continue;
                        }
                        z |= (short) (((short)b) << 8);
                        b = socket.InputStream.ReadByte();
                        if (b == 3) {
                            double[] values = {
                                1.0*x/1000.0,
                                1.0*y/1000.0,
                                1.0*z/1000.0
                            };
                            if (listener != null) {
                                listener.OnDataReceived(values);
                            }
                        }
                    }
                }
            }
            catch (ThreadAbortException) {
            }
            Console.WriteLine("Thread Finished.");
        }
    }
}