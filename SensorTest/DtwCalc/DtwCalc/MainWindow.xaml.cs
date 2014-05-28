using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Threading;
using System.Windows.Threading;
using System.Runtime.InteropServices;

namespace MYDtwTest {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        SerialPort mSerialPort;
        Dispatcher mDispatcher;

        // Trained series
        private Dictionary<string, List<Tuple<double,double>>> mTrainedSeries = new Dictionary<string, List<Tuple<double, double>>>();

        public MainWindow() {
            InitializeComponent();
            mSerialPort = new SerialPort();
            mSerialPort.BaudRate = 115200;
            mSerialPort.Parity = Parity.None;
            mSerialPort.DataBits = 8;
            mSerialPort.Handshake = Handshake.None;
            mSerialPort.StopBits = StopBits.One;

            mDispatcher = this.Dispatcher;
        }

        private void OnConectButtonClicked(object sender, RoutedEventArgs e) {
            if (mSerialPort.IsOpen) {
                ConnectButton.Content = "Connect";
                try {
                    mSerialPort.Close();
                } catch (Exception exc) {
                    MessageBox.Show(exc.Message);
                }
            } else {
                if (String.IsNullOrEmpty(SerialPortComboBox.Text)) {
                    MessageBox.Show("Could not open empty port.");
                    return;
                }

                mSerialPort.PortName = SerialPortComboBox.Text;
                //MessageBox.Show(SerialPortComboBox.Text);
                try {
                    mSerialPort.Open();
                } catch (Exception exc) {
                    MessageBox.Show(exc.Message);
                }
                ConnectButton.Content = "Close";
            }
        }

        bool TrainMode = false;
        BackgroundWorker mTrainWorker;
        private void OnTrainButtonClicked(object sender, RoutedEventArgs e) {
            if (!mSerialPort.IsOpen) {
                MessageBox.Show("Not Connected!");
                return;
            }
            if (!TrainMode) {
                TrainMode = true;
                TrainButton.Content = "Stop";

                // fetch data incoming from serial port and apply them to a serialvariable
                mTrainWorker = new BackgroundWorker();
                mTrainWorker.WorkerSupportsCancellation = true;
                mTrainWorker.DoWork += mTrainerWorker_DoWork;

                new Thread(() => {
                    for (int i = 0; i < 3; i++) {
                        mDispatcher.Invoke(() => TimerLabel.Content = (3 - i).ToString());
                        Thread.Sleep(1000);
                    }
                    mDispatcher.Invoke(() => TimerLabel.Content = "Draw gesture...");
                    mTrainWorker.RunWorkerAsync();
                }).Start();
            } else {
                // stop actions...
                mTrainWorker.CancelAsync();
                TrainButton.Content = "Train";
                TimerLabel.Content = "";
                TrainMode = false;
            }
        }

        private void GetSerialData(ref List<Tuple<double, double>> val) {
            byte[] buffer = new byte[7];
            Int16 ValX = 0, ValY = 0, ValZ = 0;
            // wait for STX - sync with device
            while (mSerialPort.ReadByte() != 0x02) { }
            mSerialPort.Read(buffer, 0, 7);

            ValX = (Int16)((buffer[1] << 8) | buffer[0]);
            ValY = (Int16)((buffer[3] << 8) | buffer[2]);
            ValZ = (Int16)((buffer[5] << 8) | buffer[4]);

            if (buffer[6] == 0x03) {
                val.Add(new Tuple<double, double>(ValX, ValY));
                mDispatcher.Invoke(() => CoordDataLabel.Content = String.Format("X: {0,10}\nY: {1,10}\nZ: {2,10}", ValX, ValY, ValZ));
            }
        }

        const int MaxLen = 400;
        void mTrainerWorker_DoWork(object sender, DoWorkEventArgs e) {
            List<Tuple<double,double>> x = new List<Tuple<double, double>>();
            mSerialPort.DiscardInBuffer();
            while (!mTrainWorker.CancellationPending) {
                GetSerialData(ref x);
            }

            if (x.Count == 0) {
                e.Cancel = true;
                return;
            }
            mTrainedSeries.Add("Serie #" + mTrainedSeries.Count, x);
            MessageBox.Show("New series added: Serie #" + mTrainedSeries.Count + " (" + x.Count + " values)");
            //NormalizeTrainedSeries();
            e.Cancel = true;
        }

        private void NormalizeTrainedSeries(int cnt = -1) {
            int min = ((cnt < 0) ? mTrainedSeries.Min(x => x.Value.Count) : cnt);
            foreach (var item in mTrainedSeries.Values) {
                if (item.Count > min)
                    // pin to fixed length
                    item.RemoveRange(min, item.Count - min);
            }
        }

        bool Capture = false;
        BackgroundWorker mTestWorker;
        private void OnTestButtonClicked(object sender, RoutedEventArgs e) {
            if (!mSerialPort.IsOpen) {
                MessageBox.Show("Not Connected!");
                return;
            }
            if (!Capture) {
                Capture = true;
                TestButton.Content = "Stop";

                // fetch data incoming from serial port and apply them to a serialvariable
                mTestWorker = new BackgroundWorker();
                mTestWorker.WorkerSupportsCancellation = true;
                mTestWorker.DoWork += mWorker_GetSeries;
                mTestWorker.RunWorkerAsync();
            } else {
                // stop actions...
                mTestWorker.CancelAsync();
                TestButton.Content = "Test";
                TimerLabel.Content = "";
                Capture = false;
            }
        }




        // define Dll functions and structures
        private struct ucr_index {
            public double   value;
            public Int64    index;
        };

        // Define imported functions
        [DllImport("DTW.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern Int32 ucr_query(double[] q, Int32 m, double r, double[] buffer, Int32 buflen, ref ucr_index result);

        // Do some live capturing in here...
        private void mWorker_GetSeries(object sender, DoWorkEventArgs e) {
            const int windowSize = 400;

            List<Tuple<double,double>> y = new List<Tuple<double, double>>(windowSize);
            mSerialPort.DiscardInBuffer();

            while (!mTestWorker.CancellationPending) {
                GetSerialData(ref y);

                // continue if not enough input data sampled...
                if (y.Count < windowSize)
                    continue;

                // resize captured data to windowSize - default nothing should be removed...
                //y.RemoveRange(windowSize, y.Count - windowSize);

                var minValue = double.MaxValue;
                Int64 ValXIndex = 0;
                Int64 ValYIndex = 0;
                string Pattern = "";

                double[] bufX = y.Select(x => x.Item1).ToArray();
                double[] bufY = y.Select(x => x.Item2).ToArray();
                ucr_index resultX = new ucr_index();
                ucr_index resultY = new ucr_index();
                // apply recorded series to all trained
                foreach (var item in mTrainedSeries) {
                    // compare each trained sequence with currently captured one - X values only atm
                    double[] trainedX = item.Value.Select(x => x.Item1).ToArray();
                    double[] trainedY = item.Value.Select(x => x.Item2).ToArray();

                    // compare x and y signals
                    if ((ucr_query(trainedX, trainedX.Length, 0.5, bufX, bufX.Length, ref resultX) < 0) /*||
                        (ucr_query(trainedY, trainedY.Length, 0.5, bufY, bufY.Length, ref resultY) < 0)*/) {
                        continue;
                    }

                    double result = resultX.value + resultY.value;
                    if (result < minValue) {
                        minValue = result;
                        ValXIndex = resultX.index;
                        ValYIndex = resultY.index;
                        Pattern = item.Key;
                    }
                }

                if (minValue < 3.0) {
                    mDispatcher.Invoke(() => {
                        TimerLabel.Content = Pattern + " DETECTED" + "\nCost: " + minValue;
                        if (Pattern == "Serie #0") {
                            DetectionLight1.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00, 0xFF, 0x00));
                        } else if (Pattern == "Serie #1") {
                            DetectionLight2.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00, 0xFF, 0x00));
                        } else if (Pattern == "Serie #2") {
                            DetectionLight3.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00, 0xFF, 0x00));
                        } else {
                            DetectionLightx.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00, 0xFF, 0x00));
                        }
                    });

                    // clear found pattern from sampled signal to ensure that it is only detected once
                    int cnt = mTrainedSeries[Pattern].Count;
                    int start = (int)Math.Min(ValXIndex, ValYIndex);
                    for (int i = start; i < start + cnt; i++) {
                        y[i] = y[start];
                    }
                    //y.RemoveRange(start, cnt);
                    //y.InsertRange(start, new List<Tuple<double, double>>(cnt));

                } else {
                    // print result
                    mDispatcher.Invoke(() => {
                        TimerLabel.Content = Pattern + "\nCost: " + minValue;
                        DetectionLight1.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x00, 0x00));
                        DetectionLight2.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x00, 0x00));
                        DetectionLight3.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x00, 0x00));
                        DetectionLightx.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x00, 0x00));
                    });
                }

                //y.RemoveAt(0);
                y.RemoveRange(0, 50);       // discard values for a half second
            }

            e.Cancel = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void PropChanged(string s) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(s));
            }
        }
    }
}
