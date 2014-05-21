using System.Collections.Generic;

namespace PresIt.Android.GestureRecognition.Classifier.FeatureExtraction {
    public class NormExtractor : IFeatureExtractor {
        public Gesture SampleSignal(Gesture signal) {
            var sampledValues = new List<double[]>();
            var sampledSignal = new Gesture(sampledValues, signal.Label);

            var min = double.MaxValue;
            var max = double.MinValue;

            for (var i = 0; i < signal.Length; ++i) {
                for (var j = 0; j < 3; ++j) {
                    if (signal.GetValue(i, j) > max) max = signal.GetValue(i, j);
                    if (signal.GetValue(i, j) < min) min = signal.GetValue(i, j);
                }
            }

            for (var i = 0; i < signal.Length; ++i) {
                sampledValues.Add(new double[3]);
                for (var j = 0; j < 3; ++j) {
                    sampledSignal.SetValue(i, j, (signal.GetValue(i, j) - min) / (max - min));
                }
            }

            return sampledSignal;
        }
    }
}