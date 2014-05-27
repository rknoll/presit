using System;
using System.Collections.Generic;
using Android.Content;
using Android.Hardware;
using PresIt.Android.GestureRecognition.Sensors;
using ISensorListener = PresIt.Android.GestureRecognition.Sensors.ISensorListener;

namespace PresIt.Android.GestureRecognition.Recorder {
    public class GestureRecorder :ISensorListener {

        public enum RecordMode {
            MotionDetected,
            PushToGesture
        }

        private const int MinGestureSize = 8;
        public double Threshold { get; set; }
        private bool isRecording;
        private int stepsSinceNoMovement;
        private List<double[]> gestureValues;
        private IGestureRecorderListener listener;
        private readonly ISensorSource source;
        public bool IsRunning { get; private set; }
        public RecordMode CurrentRecordMode { get; set; }

        public GestureRecorder(ISensorSource source) {
            this.source = source;
            CurrentRecordMode = RecordMode.MotionDetected;
            Threshold = 2;
        }

        private double CalcVectorNorm(double[] values) {
            return
                Math.Sqrt(values[0]*values[0] + values[1]*values[1] +
                          values[2]*values[2]) - 9.9;
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

        public void RegisterListener(IGestureRecorderListener listener) {
            this.listener = listener;
            Start();
        }

        public void UnregisterListener(IGestureRecorderListener listener) {
            this.listener = null;
            Stop();
        }

        public void Start() {
            source.SetSensorListener(this);
            IsRunning = true;
        }

        public void Stop() {
            source.SetSensorListener(null);
            IsRunning = false;
        }

        public void Pause(bool b) {
            if (b) {
                Stop();
            } else {
                Start();
            }
        }

        public void OnDataReceived(double[] values) {
            switch (CurrentRecordMode) {
                case RecordMode.MotionDetected:
                    if (isRecording) {
                        gestureValues.Add(values);
                        if (CalcVectorNorm(values) < Threshold) {
                            stepsSinceNoMovement ++;
                        } else {
                            stepsSinceNoMovement = 0;
                        }
                    } else if(CalcVectorNorm(values) >= Threshold) {
                        isRecording = true;
                        stepsSinceNoMovement = 0;
                        gestureValues = new List<double[]>();
                        gestureValues.Add(values);
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
                        gestureValues.Add(values);
                    }
                    break;
            }
        }
    }
}