using System.Collections.Generic;
using Java.Lang;

namespace PresIt.Android.GestureRecognition.Classifier {
    public static class DTWAlgorithm {
        private const double OffsetPenalty = 0.5;

        private static double PNorm(IEnumerable<double> vector, int p) {
            double result = 0;
            foreach (var b in vector) {
                double sum = 1;
                for (var i = 0; i < p; ++i) {
                    sum *= b;
                }
                result += sum;
            }
            return Math.Pow(result, 1.0 / p);
        }

        public static double CalcDistance(Gesture a, Gesture b) {
            int signalDimensions = a.Values[0].Length;
            int signal1Length = a.Length;
            int signal2Length = b.Length;

            var distMatrix = new double[signal1Length, signal2Length];
            var costMatrix = new double[signal1Length, signal2Length];

            for (var i = 0; i < signal1Length; ++i) {
                for (var j = 0; j < signal2Length; ++j) {
                    var vec = new List<double>();
                    for (var k = 0; k < signalDimensions; ++k) {
                        vec.Add(a.GetValue(i, k) - b.GetValue(j, k));
                    }
                    distMatrix[i, j] = PNorm(vec, 2);
                }
            }

            for (var i = 0; i < signal1Length; ++i) {
                costMatrix[i, 0] = distMatrix[i, 0];
            }

            for (var j = 1; j < signal2Length; ++j) {
                for (var i = 0; i < signal1Length; ++i) {
                    if (i == 0) {
                        costMatrix[i, j] = costMatrix[i, j - 1] + distMatrix[i, j];
                    } else {
                        double minCost, cost;
                        minCost = costMatrix[i - 1, j - 1] + distMatrix[i, j];
                        if ((cost = costMatrix[i - 1, j] + distMatrix[i, j]) < minCost) {
                            minCost = cost + OffsetPenalty;
                        }
                        if ((cost = costMatrix[i, j - 1] + distMatrix[i, j]) < minCost) {
                            minCost = cost + OffsetPenalty;
                        }
                        costMatrix[i, j] = minCost;
                    }
                }
            }

            return costMatrix[signal1Length - 1, signal2Length - 1];
        }

    }
}