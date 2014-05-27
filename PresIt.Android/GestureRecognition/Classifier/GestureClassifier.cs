using System.Collections.Generic;
using System.Linq;
using PresIt.Android.GestureRecognition.Classifier.FeatureExtraction;

namespace PresIt.Android.GestureRecognition.Classifier {
    public class GestureClassifier {
        private List<Gesture> trainingSet;
        public IFeatureExtractor FeatureExtractor { get; set; }

        public GestureClassifier(IFeatureExtractor extractor) {
            trainingSet = new List<Gesture>();
            FeatureExtractor = extractor;
        }

        public bool TrainData(Gesture signal) {
            trainingSet.Add(FeatureExtractor.SampleSignal(signal));
            return true;
        }

        public bool CheckForLabel(string label) {
            return trainingSet.Any(s => s.Label == label);
        }

        public bool DeleteTrainingSet() {
            trainingSet = new List<Gesture>();
            return true;
        }

        public bool DeleteLabel(string label) {
            var items = trainingSet.Where(s => s.Label != label).ToList();
            var removed = items.Count != trainingSet.Count;
            trainingSet = items;
            return removed;
        }

        public IEnumerable<string> GetLabels() {
            return trainingSet.Select(s => s.Label).Distinct();
        }

        public Distribution ClassifySignal(Gesture signal) {
            var distribution = new Distribution();
            var sampledSignal = FeatureExtractor.SampleSignal(signal);
            foreach (var s in trainingSet) {
                distribution.AddEntry(s.Label, DTWAlgorithm.CalcDistance(s, sampledSignal));
            }
            return distribution;
        }

    }
}