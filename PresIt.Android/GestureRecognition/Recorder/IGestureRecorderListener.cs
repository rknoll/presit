using System.Collections.Generic;

namespace PresIt.Android.GestureRecognition.Recorder {
    public interface IGestureRecorderListener {
        void OnGestureRecorded(List<double[]> values);
    }
}