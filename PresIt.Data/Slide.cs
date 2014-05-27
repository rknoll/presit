using System.ComponentModel;
using System.Runtime.Serialization;

namespace PresIt.Data {

    /// <summary>
    /// Represents a Slide of a Presentation
    /// </summary>
    [DataContract]
    public class Slide : INotifyPropertyChanged {

        private byte[] imageData;
        private int slideNumber;

        /// <summary>
        /// Raw Image data as byte array (encoded as PNG)
        /// </summary>
        [DataMember]
        public byte[] ImageData {
            get { return imageData; }
            set {
                if (imageData == value) return;
                imageData = value;
                OnPropertyChanged("ImageData");
            }
        }

        /// <summary>
        /// Number of this Slide in the Presentation (starts with 1)
        /// </summary>
        [DataMember]
        public int SlideNumber {
            get { return slideNumber; }
            set {
                if (slideNumber == value) return;
                slideNumber = value;
                OnPropertyChanged("SlideNumber");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}