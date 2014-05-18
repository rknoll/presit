using System.ComponentModel;
using System.Runtime.Serialization;

namespace PresIt.Data {
    [DataContract]
    public class Slide : INotifyPropertyChanged {

        private byte[] imageData;

        private int slideNumber;

        [DataMember]
        public byte[] ImageData {
            get { return imageData; }
            set {
                if (imageData == value) return;
                imageData = value;
                OnPropertyChanged("ImageData");
            }
        }
        
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