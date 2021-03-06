using System;
using System.Collections.Generic;

namespace PresIt.Android.GestureRecognition {
    [Serializable]
    public class Gesture {
        public string Label { get; set; }
        public List<double[]> Values { get; set; }
        
        // default ctor for de/serializer
        public Gesture() {
        }

        public Gesture(List<double[]> values, string label) {
            Values = values;
            Label = label;
        }

        public double GetValue(int index, int dim) {
            return Values[index][dim];
        }
        
        public void SetValue(int index, int dim, double value) {
            Values[index][dim] = value;
        }

        public int Length {
            get { return Values.Count; }
        }
    }
}