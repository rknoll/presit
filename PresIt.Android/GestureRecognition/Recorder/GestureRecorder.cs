using System;
using System.Collections.Generic;
using Android.Content;
using Android.Hardware;

namespace PresIt.Android.GestureRecognition.Recorder {
    public class GestureRecorder : Java.Lang.Object, ISensorEventListener {

        public enum RecordMode {
            MotionDetected,
            PushToGesture
        }

        private const int MinGestureSize = 8;
        public double Threshold { get; set; }
        private SensorManager sensorManager;
        private bool isRecording;
        private int stepsSinceNoMovement;
        private List<double[]> gestureValues;
        private readonly Context context;
        private IGestureRecorderListener listener;
        public bool IsRunning { get; private set; }
        public RecordMode CurrentRecordMode { get; set; }

        public void OnAccuracyChanged(Sensor sensor, SensorStatus accuracy) {
        }

        public GestureRecorder(Context context) {
            this.context = context;
            CurrentRecordMode = RecordMode.MotionDetected;
            Threshold = 2;
        }

        private double CalcVectorNorm(SensorEvent sensorEvent) {
            return
                Math.Sqrt(sensorEvent.Values[0]*sensorEvent.Values[0] + sensorEvent.Values[1]*sensorEvent.Values[1] +
                          sensorEvent.Values[2]*sensorEvent.Values[2]) - 9.9;
        }

        public void OnPushToGesture(bool pushed) {
            if (CurrentRecordMode == RecordMode.PushToGesture) {
                isRecording = pushed;
                if (isRecording) {
                    gestureValues = new List<double[]>();
                } else {
                    if (gestureValues.Count > MinGestureSize) {
                        listener.OnGestureRecorded(gestureValues);
                    }
                    gestureValues = null;
                }
            }
        }

        public void OnSensorChanged(SensorEvent e) {
            var value = new double[] {
                e.Values[0],
                e.Values[1],
                e.Values[2]
            };
            switch (CurrentRecordMode) {
                case RecordMode.MotionDetected:
                    if (isRecording) {
                        gestureValues.Add(value);
                        if (CalcVectorNorm(e) < Threshold) {
                            stepsSinceNoMovement ++;
                        } else {
                            stepsSinceNoMovement = 0;
                        }
                    } else if(CalcVectorNorm(e) >= Threshold) {
                        isRecording = true;
                        stepsSinceNoMovement = 0;
                        gestureValues = new List<double[]>();
                        gestureValues.Add(value);
                    }
                    if (stepsSinceNoMovement == 10) {
                        if (gestureValues.Count - 10 > MinGestureSize) {
                            listener.OnGestureRecorded(gestureValues.GetRange(0, gestureValues.Count - 10));
                        }
                        gestureValues = null;
                        stepsSinceNoMovement = 0;
                        isRecording = false;
                    }
                    break;
                case RecordMode.PushToGesture:
                    if (isRecording) {
                        gestureValues.Add(value);
                    }
                    break;
            }
        }

        public void RegisterListener(IGestureRecorderListener listener) {
            this.listener = listener;
            Start();
        }

        public void UnregisterListener(IGestureRecorderListener listener) {
            this.listener = null;
            Stop();
        }

        public void Start() {
            sensorManager = (SensorManager)context.GetSystemService(Context.SensorService);
            sensorManager.RegisterListener(this, sensorManager.GetDefaultSensor(SensorType.Accelerometer),
                SensorDelay.Game);
            IsRunning = true;
        }

        public void Stop() {
            sensorManager.UnregisterListener(this);
            IsRunning = false;
        }

        public void Pause(bool b) {
            if (b) {
                sensorManager.UnregisterListener(this);
            } else {
                sensorManager.RegisterListener(this, sensorManager.GetDefaultSensor(SensorType.Accelerometer),
                SensorDelay.Game);
            }
        }
    }
}