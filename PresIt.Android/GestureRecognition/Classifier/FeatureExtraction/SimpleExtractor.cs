namespace PresIt.Android.GestureRecognition.Classifier.FeatureExtraction {
    public class SimpleExtractor : IFeatureExtractor {
        public Gesture SampleSignal(Gesture signal) {
            return signal;
        }
    }
}