using System.ComponentModel;
using System.Runtime.Serialization;

namespace PresIt.Data {
    [DataContract]
    public class Slide : INotifyPropertyChanged {

        private byte[] imageData;

        [DataMember]
        public byte[] ImageData {
            get { return imageData; }
            set {
                if (imageData == value) return;
                imageData = value;
                OnPropertyChanged("ImageData");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}