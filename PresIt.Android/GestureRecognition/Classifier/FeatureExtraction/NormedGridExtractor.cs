namespace PresIt.Android.GestureRecognition.Classifier.FeatureExtraction {
    public class NormedGridExtractor : IFeatureExtractorConstCount {
        public Gesture SampleSignal(Gesture signal) {
            var s = new GridExtractor().SampleSignal(signal);
            return new NormExtractor().SampleSignal(s);
        }
    }
}