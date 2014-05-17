using System.Windows.Input;

namespace PresIt.Windows {
    public interface IMainWindowPresenter {
        ICommand NewPresentationCommand { get; }
    }
}