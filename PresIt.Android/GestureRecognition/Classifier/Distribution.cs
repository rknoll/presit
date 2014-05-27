using System.Collections.Generic;

namespace PresIt.Android.GestureRecognition.Classifier {
    public class Distribution {
        private readonly Dictionary<string, double> distribution;
        public string BestMatch { get; private set; }
        public double BestDistance { get; private set; }

        public Distribution() {
            distribution = new Dictionary<string, double>();
            BestDistance = double.MaxValue;
        }

        public void AddEntry(string tag, double distance) {
            if (!distribution.ContainsKey(tag) || distance < distribution[tag]) {
                distribution[tag] = distance;
                if (distance < BestDistance) {
                    BestDistance = distance;
                    BestMatch = tag;
                }
            }
        }

        public int Size {
            get { return distribution.Count; }
        }

    }
}