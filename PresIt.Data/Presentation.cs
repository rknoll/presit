using System.ComponentModel;
using System.Runtime.Serialization;

namespace PresIt.Data {
    [DataContract]
    public class Presentation : INotifyPropertyChanged {

        private string owner;

        private string name;

        [DataMember]
        public string Owner {
            get { return owner; }
            set {
                if (owner == value) return;
                owner = value;
                OnPropertyChanged("Owner");
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName) {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}