using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.IO;
using PresIt.Data;

namespace PresIt.Windows {
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow {
        private readonly SynchronizationContext context;
        private IMainWindowPresenter dataContext;

        public MainWindow() {
            InitializeComponent();
            context = SynchronizationContext.Current;

            OverlayRectangle.Opacity = 0.6;
            OverlayRectangle.Visibility = Visibility.Visible;
            LoginGrid.Visibility = Visibility.Visible;
            NewPresentationGrid.Visibility = Visibility.Hidden;
        }

        private void OnNewPresentationDrop(object sender, DragEventArgs e) {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var files = (string[]) e.Data.GetData(DataFormats.FileDrop);
            if (files.Length != 1 || string.IsNullOrEmpty(files[0])) return;
            var file = Path.GetFileName(files[0]);
            if (string.IsNullOrEmpty(file)) return;
            if (file.Contains(".")) {
                file = file.Substring(0, file.LastIndexOf('.'));
            }
            var be = NewPresentationNameTextBox.GetBindingExpression(TextBox.TextProperty);
            NewPresentationNameTextBox.Text = file;
            if(be != null) be.UpdateSource();
            ShowNewPresentation();
        }

        private void OnNewPresentationClick(object sender, RoutedEventArgs e) {
            ShowNewPresentation();
        }

        private void ShowNewPresentation() {
            OverlayRectangle.Opacity = 0;
            OverlayRectangle.Visibility = Visibility.Visible;
            NewPresentationGrid.SetValue(Canvas.TopProperty, -NewPresentationContent.ActualHeight);
            NewPresentationGrid.Visibility = Visibility.Visible;

            var opacityAnimation = new DoubleAnimation {
                From = 0,
                To = 0.6,
                Duration = TimeSpan.FromSeconds(0.3)
            };
            var topAnimation = new DoubleAnimation {
                From = -NewPresentationContent.ActualHeight,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.3)
            };

            var storyboard = new Storyboard();

            Storyboard.SetTarget(opacityAnimation, OverlayRectangle);
            Storyboard.SetTarget(topAnimation, NewPresentationGrid);
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(OpacityProperty));
            Storyboard.SetTargetProperty(topAnimation, new PropertyPath(Canvas.TopProperty));

            storyboard.Children.Add(opacityAnimation);
            storyboard.Children.Add(topAnimation);

            // start animation
            storyboard.Begin();
        }
        
        private void HideNewPresentationScreen() {
            OverlayRectangle.Opacity = 0.6;
            OverlayRectangle.Visibility = Visibility.Visible;
            NewPresentationGrid.SetValue(Canvas.TopProperty, 0.0);
            NewPresentationGrid.Visibility = Visibility.Visible;

            var opacityAnimation = new DoubleAnimation {
                From = 0.6,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.3)
            };
            var topAnimation = new DoubleAnimation {
                From = 0,
                To = -NewPresentationContent.ActualHeight,
                Duration = TimeSpan.FromSeconds(0.3)
            };

            var storyboard = new Storyboard();

            Storyboard.SetTarget(opacityAnimation, OverlayRectangle);
            Storyboard.SetTarget(topAnimation, NewPresentationGrid);
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(OpacityProperty));
            Storyboard.SetTargetProperty(topAnimation, new PropertyPath(Canvas.TopProperty));

            storyboard.Children.Add(opacityAnimation);
            storyboard.Children.Add(topAnimation);
            
            storyboard.Completed += (sender1, eventArgs) => {
                OverlayRectangle.Visibility = Visibility.Collapsed;
                NewPresentationGrid.Visibility = Visibility.Hidden;
            };

            // start animation
            storyboard.Begin();
        }

        private void HideLoginScreen() {
            CommandManager.InvalidateRequerySuggested();

            OverlayRectangle.Opacity = 0.6;
            OverlayRectangle.Visibility = Visibility.Visible;

            var opacityAnimation1 = new DoubleAnimation {
                From = 0.6,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.3)
            };
                
            var opacityAnimation2 = new DoubleAnimation {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.3)
            };

            var storyboard = new Storyboard();

            Storyboard.SetTarget(opacityAnimation1, OverlayRectangle);
            Storyboard.SetTarget(opacityAnimation2, LoginGrid);
            Storyboard.SetTargetProperty(opacityAnimation1, new PropertyPath(OpacityProperty));
            Storyboard.SetTargetProperty(opacityAnimation2, new PropertyPath(OpacityProperty));

            storyboard.Children.Add(opacityAnimation1);
            storyboard.Children.Add(opacityAnimation2);

            storyboard.Completed += (sender1, eventArgs) => {
                OverlayRectangle.Visibility = Visibility.Collapsed;
                LoginGrid.Visibility = Visibility.Collapsed;
            };

            // start animation
            storyboard.Begin();
        }

        private void OnMainWindowSourceInitialized(object sender, EventArgs e) {
            dataContext = DataContext as IMainWindowPresenter;
            if (dataContext == null) return;
            dataContext.IsAuthenticated += (o, args) => context.Post(state => HideLoginScreen(), null);
            dataContext.EditPresentation += (o, pres) => context.Post(state => EditPresentation(pres), null);
            dataContext.PresentationList += (o, pres) => context.Post(state => ShowSelectPresentation(pres), null);
            dataContext.PresentationSaved += (o, pres) => context.Post(state => HideEditPresentation(), null);
            dataContext.PresentationDeleted += (o, pres) => context.Post(state => HideEditPresentation(), null);
            dataContext.ShowPresentation += (o, pres) => context.Post(state => ShowPresentation(pres), null);
        }

        private void HideEditPresentation() {
            EditPresentationView.Visibility = Visibility.Hidden;
        }

        private void EditPresentation(Presentation pres) {
            if (pres != null) {
                EditPresentationView.Visibility = Visibility.Visible;
                EditPresentationName.Content = pres.Name;

                var slides = new ObservableCollection<SlidePreview>();

                foreach (var slide in pres.Slides) {
                    slides.Add(new SlidePreview(slide, pres.Id));
                }

                slides.Add(new SlidePreview(null, pres.Id));

                EditPresentationSlides.ItemsSource = slides;
            }

            if (NewPresentationGrid.Visibility == Visibility.Visible) {
                HideNewPresentationScreen();
            }
        }

        private void OnCancelNewPresentationClick(object sender, RoutedEventArgs e) {
            HideNewPresentationScreen();
        }
        
        private void ShowSelectPresentation(IEnumerable<PresentationPreview> presentationList) {
            SelectPresentationView.Visibility = Visibility.Visible;

            var presentations = new ObservableCollection<SlidePreview>();

            foreach (var presentation in presentationList) {
                presentations.Add(new SlidePreview(presentation.FirstSlide, presentation.Id, presentation.Name));
            }

            SelectPresentationList.ItemsSource = presentations;
        }
        
        private void HideSelectPresentation() {
            SelectPresentationView.Visibility = Visibility.Hidden;
        }

        private void OnCancelShowPresentationClick(object sender, RoutedEventArgs e) {
            HideSelectPresentation();
        }

        private void OnSelectPresentationListDoubleClick(object sender, MouseButtonEventArgs e) {
            var slidePreview = SelectPresentationList.SelectedItem as SlidePreview;
            if (slidePreview == null || slidePreview.PresentationId == null) return;
            dataContext.GetPresentation(slidePreview.PresentationId);
        }

        private void ShowPresentation(Presentation pres) {
            var presentationView = new PresentationWindow();
            presentationView.DataContext = pres;
            presentationView.Show();
        }
    }
}