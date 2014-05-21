using System.Collections.Generic;

namespace PresIt.Android.GestureRecognition.Classifier.FeatureExtraction {
    public class GridExtractor : IFeatureExtractorConstCount {
        private const int SampleSteps = 32;

        public Gesture SampleSignal(Gesture signal) {
            var sampledValues = new List<double[]>();
            var sampledSignal = new Gesture(sampledValues, signal.Label);

            for (var j = 0; j < SampleSteps; ++j) {
                sampledValues.Add(new double[3]);
                for (var i = 0; i < 3; ++i) {
                    var findex = 1.0 * (signal.Length - 1) * j / (SampleSteps - 1);
                    var res = findex - (int) findex;
                    sampledSignal.SetValue(j, i, (1-res) * signal.GetValue((int)findex, i) + ((int)findex + 1 < signal.Length - 1 ? res * signal.GetValue((int)findex + 1, i) : 0));
                }
            }

            return sampledSignal;
        }
    }
}