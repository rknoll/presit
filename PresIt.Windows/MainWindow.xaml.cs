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
        private string droppedFileName;
        private Presentation currentPresentation;

        public MainWindow() {
            InitializeComponent();
            context = SynchronizationContext.Current;

            OverlayRectangle.Opacity = 0.6;
            OverlayRectangle.Visibility = Visibility.Visible;
            LoginGrid.Visibility = Visibility.Visible;
            NewPresentationGrid.Visibility = Visibility.Hidden;
        }

        private void OnNewPresentationDrop(object sender, DragEventArgs e) {
            droppedFileName = null;
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var files = (string[]) e.Data.GetData(DataFormats.FileDrop);
            if (files.Length != 1 || string.IsNullOrEmpty(files[0])) return;
            droppedFileName = files[0];
            var file = Path.GetFileName(files[0]);
            if (string.IsNullOrEmpty(file)) {
                droppedFileName = null;
                return;
            }
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
            droppedFileName = null;
            var be = NewPresentationNameTextBox.GetBindingExpression(TextBox.TextProperty);
            NewPresentationNameTextBox.Text = "";
            if (be != null) be.UpdateSource();
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
            currentPresentation = pres;

            if (pres != null) {
                EditPresentationView.Visibility = Visibility.Visible;
                EditPresentationName.Content = pres.Name;
                ImportSlides();
            }

            if (NewPresentationGrid.Visibility == Visibility.Visible) {
                HideNewPresentationScreen();
            }
        }

        private void ImportSlides(int slideIndex = -1) {
            var fileName = droppedFileName;
            droppedFileName = null;
            if (currentPresentation == null) return;

            var importedSlides = new List<Slide>();
            int slideNumber = 1;

            if (currentPresentation.Slides != null) {
                foreach (var slide in currentPresentation.Slides) {
                    slideNumber++;
                    importedSlides.Add(slide);
                }
            }

            if (fileName != null) {
                ISlidesImporter importer = null;
                if (fileName.ToLower().EndsWith(".ppt") ||
                    fileName.ToLower().EndsWith(".pptx")) {
                    importer = new PowerPointImporter();
                } else if (fileName.ToLower().EndsWith(".jpg") ||
                            fileName.ToLower().EndsWith(".jpeg") ||
                            fileName.ToLower().EndsWith(".png") ||
                            fileName.ToLower().EndsWith(".bmp")) {
                    importer = new ImageImporter();
                }

                if (importer != null) {
                    foreach (var slideData in importer.Convert(fileName)) {
                        importedSlides.Insert(slideIndex != -1 ? (slideIndex-1) : slideNumber-1, new Slide {
                            ImageData = slideData,
                            SlideNumber = slideIndex != -1 ? slideIndex++ : slideNumber++
                        });
                        if (slideIndex != -1) slideNumber++;
                    }
                }
            }

            if (slideIndex != -1) {
                for (slideIndex--; slideIndex < slideNumber - 1; ++slideIndex) {
                    importedSlides[slideIndex].SlideNumber = slideIndex + 1;
                }
            }

            currentPresentation.Slides = importedSlides;

            var slides = new ObservableCollection<SlidePreview>();

            foreach (var slide in importedSlides) {
                slides.Add(SlidePreview.CreateFromSlide(slide, currentPresentation.Id));
            }

            slides.Add(SlidePreview.CreateAddNewSlide(currentPresentation.Id));

            EditPresentationSlides.ItemsSource = slides;
        }

        private void OnCancelNewPresentationClick(object sender, RoutedEventArgs e) {
            HideNewPresentationScreen();
        }
        
        private void ShowSelectPresentation(IEnumerable<PresentationPreview> presentationList) {
            SelectPresentationView.Visibility = Visibility.Visible;

            var presentations = new ObservableCollection<SlidePreview>();

            foreach (var presentation in presentationList) {
                presentations.Add(SlidePreview.CreateFromSlide(presentation.FirstSlide, presentation.Id, presentation.Name));
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
            dataContext.StartPresentation(slidePreview.PresentationId);
        }

        private void ShowPresentation(Presentation pres) {
            var presentationView = new PresentationWindow(dataContext);
            presentationView.DataContext = pres;
            presentationView.ShowDialog();
            dataContext.StopPresentation();
        }

        private void OnEditPresentationSlideDrop(object sender, DragEventArgs e) {
            if (currentPresentation == null) return;
            var img = sender as Image;
            if(img == null) return;
            var preview = img.DataContext as SlidePreview;
            if (preview == null) return;

            droppedFileName = null;
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var files = (string[]) e.Data.GetData(DataFormats.FileDrop);
            if (files.Length != 1 || string.IsNullOrEmpty(files[0])) return;
            droppedFileName = files[0];
            var file = Path.GetFileName(files[0]);
            if (string.IsNullOrEmpty(file)) {
                droppedFileName = null;
                return;
            }

            int slideIndex;
            if (!int.TryParse(preview.SlideText, out slideIndex)) slideIndex = -1;

            ImportSlides(slideIndex);
        }
    }
}