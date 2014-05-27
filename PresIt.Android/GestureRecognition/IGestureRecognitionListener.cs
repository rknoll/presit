using PresIt.Android.GestureRecognition.Classifier;

namespace PresIt.Android.GestureRecognition {
    public interface IGestureRecognitionListener {
        void OnGestureRecognized(GestureRecognitionService source, Distribution distribution);
        void OnGestureLearned(GestureRecognitionService source, string gestureName);
        void OnTrainingSetDeleted(GestureRecognitionService source);
    }
}