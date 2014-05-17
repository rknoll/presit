using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace PresIt.Data {
    [DataContract]
    public class Presentation : INotifyPropertyChanged {

        private string id;

        private string owner;

        private string name;

        private IEnumerable<Slide> slides;

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
        
        [DataMember]
        public IEnumerable<Slide> Slides {
            get { return slides; }
            set {
                if (slides == value) return;
                slides = value;
                OnPropertyChanged("Slides");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName) {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}