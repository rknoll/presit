using System;
using PresIt.Data;

namespace PresIt.Windows {
    public interface IMainWindowPresenter {
        event EventHandler IsAuthenticated;
        event EventHandler<Presentation> EditPresentation;
    }
}