using Android.Content;
using Android.Hardware;

namespace PresIt.Android.GestureRecognition.Sensors {
    public class PhoneSensorSource : Java.Lang.Object, ISensorEventListener, ISensorSource {
        private ISensorListener listener;
        private readonly Context context;
        private SensorManager sensorManager;

        public PhoneSensorSource(Context context) {
            this.context = context;
        }

        public void SetSensorListener(ISensorListener l) {
            listener = l;
            if (listener != null) {
                sensorManager = (SensorManager) context.GetSystemService(Context.SensorService);
                sensorManager.RegisterListener(this, sensorManager.GetDefaultSensor(SensorType.Accelerometer), SensorDelay.Game);
            } else {
                if (sensorManager != null) {
                    sensorManager.UnregisterListener(this);
                    sensorManager = null;
                }
            }
        }

        public void OnAccuracyChanged(Sensor sensor, SensorStatus accuracy) {
        }

        public void OnSensorChanged(SensorEvent e) {
            var values = new double[] {
                e.Values[0],
                e.Values[1],
                e.Values[2]
            };
             if (listener != null) {
                 listener.OnDataReceived(values);
            }
        }
    }
}