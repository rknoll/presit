using System.Collections.Generic;
using PresIt.Android.GestureRecognition.Classifier;
using PresIt.Android.GestureRecognition.Classifier.FeatureExtraction;
using PresIt.Android.GestureRecognition.Recorder;
using PresIt.Android.GestureRecognition.Sensors;

namespace PresIt.Android.GestureRecognition {
    public class GestureRecognitionService : IGestureRecorderListener {

        private readonly GestureRecorder recorder;
        private readonly GestureClassifier classifier;
        private string activeLearnLabel;
        public bool IsLearning { get; private set; }
        private bool isClassifying;

        private readonly HashSet<IGestureRecognitionListener> listeners;

        public GestureRecognitionService(ISensorSource source) {
            listeners = new HashSet<IGestureRecognitionListener>();
            recorder = new GestureRecorder(source);
            classifier = new GestureClassifier(new NormedGridExtractor());
            recorder.RegisterListener(this);
        }

        public void OnGestureRecorded(List<double[]> values) {
            if (IsLearning) {
                classifier.TrainData(new Gesture(values, activeLearnLabel));
                foreach (var listener in listeners) {
                    listener.OnGestureLearned(this, activeLearnLabel);
                }
            } else if (isClassifying) {
                recorder.Pause(true);
                var distribution = classifier.ClassifySignal(new Gesture(values, null));
                recorder.Pause(false);
                if (distribution != null && distribution.Size > 0) {
                    foreach (var listener in listeners) {
                        listener.OnGestureRecognized(this, distribution);
                    }
                }
            }
        }

        public void DeleteTrainingSet() {
			if (classifier.DeleteTrainingSet()) {
				foreach (var listener in listeners) {
					listener.OnTrainingSetDeleted(this);
				}
			}
		}

		public void OnPushToGesture(bool pushed) {
			recorder.OnPushToGesture(pushed);
		}

		public void RegisterListener(IGestureRecognitionListener listener) {
			if (listener != null) {
				listeners.Add(listener);
			}
		}

		public void StartClassificationMode() {
			isClassifying = true;
			recorder.Start();
		}

		public void StartLearnMode(string gestureName) {
			activeLearnLabel = gestureName;
			IsLearning = true;
		}

		public void StopLearnMode() {
			IsLearning = false;
		}

		public void UnregisterListener(IGestureRecognitionListener listener) {
			listeners.Remove(listener);
			if (listeners.Count == 0) {
				StopClassificationMode();
			}
		}

		public IEnumerable<string> GetGestureList() {
			return classifier.GetLabels();
		}

		public void StopClassificationMode() {
			isClassifying = false;
			recorder.Stop();
		}

		public void DeleteGesture(string gestureName) {
			classifier.DeleteLabel(gestureName);
		}

		public void SetThreshold(double threshold) {
			recorder.Threshold = threshold;
		}
    }
}