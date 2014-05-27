using System.ComponentModel;
using System.Runtime.Serialization;

namespace PresIt.Data {
    [DataContract]
    public class PresentationPreview : INotifyPropertyChanged {

        private string id;

        private string name;

        private Slide firstSlide;

        [DataMember]
        public string Id {
            get { return id; }
            set {
                if (id == value) return;
                id = value;
                OnPropertyChanged("Id");
            }
        }

        [DataMember]
        public string Name {
            get { return name; }
            set {
                if (name == value) return;
                name = value;
                OnPropertyChanged("Name");
            }
        }
        
        [DataMember]
        public Slide FirstSlide {
            get { return firstSlide; }
            set {
                if (firstSlide == value) return;
                firstSlide = value;
                OnPropertyChanged("FirstSlide");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName) {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
        
    }
}