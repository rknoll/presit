namespace PresIt.Android.GestureRecognition.Classifier.FeatureExtraction {
    public interface IFeatureExtractor {
        Gesture SampleSignal(Gesture signal);
    }
}