using System.ComponentModel;
using System.Runtime.Serialization;

namespace PresIt.Data {

    /// <summary>
    /// Represents the Preview of a Presentation
    /// </summary>
    [DataContract]
    public class PresentationPreview : INotifyPropertyChanged {

        private string id;
        private string name;
        private Slide firstSlide;

        /// <summary>
        /// ID of the Presentation
        /// </summary>
        [DataMember]
        public string Id {
            get { return id; }
            set {
                if (id == value) return;
                id = value;
                OnPropertyChanged("Id");
            }
        }

        /// <summary>
        /// Name of the Presentation
        /// </summary>
        [DataMember]
        public string Name {
            get { return name; }
            set {
                if (name == value) return;
                name = value;
                OnPropertyChanged("Name");
            }
        }

        /// <summary>
        /// The first Slide of the Presentation
        /// </summary>
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