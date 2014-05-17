using System.Windows;

namespace PresIt.Windows {
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App {
        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);

            var view = new MainWindow();
            var presenter = new MainWindowPresenter();
            view.DataContext = presenter;
            view.Show();
        }
    }
}