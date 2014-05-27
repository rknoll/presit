using PresIt.Android.GestureRecognition.Classifier;

namespace PresIt.Android.GestureRecognition {
    public interface IGestureRecognitionListener {
        void OnGestureRecognized(Distribution distribution);
        void OnGestureLearned(string gestureName);
        void OnTrainingSetDeleted();
    }
}