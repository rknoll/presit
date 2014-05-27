﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
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

            storyboard.Begin();
        }

        private void HideNewPresentationScreen(bool fast = false) {
            var be = NewPresentationNameTextBox.GetBindingExpression(TextBox.TextProperty);
            NewPresentationNameTextBox.Text = "";
            if (be != null) be.UpdateSource();
            if (!fast) {
                OverlayRectangle.Opacity = 0.6;
                OverlayRectangle.Visibility = Visibility.Visible;
            }
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

            if (!fast) {
                storyboard.Children.Add(opacityAnimation);
            }

            storyboard.Children.Add(topAnimation);
            
            storyboard.Completed += (sender1, eventArgs) => {
                if (!fast) {
                    OverlayRectangle.BeginAnimation(OpacityProperty, null);
                    OverlayRectangle.Visibility = Visibility.Hidden;
                }

                NewPresentationGrid.Visibility = Visibility.Hidden;
            };

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
                OverlayRectangle.BeginAnimation(OpacityProperty, null);
                OverlayRectangle.Visibility = Visibility.Hidden;
                LoginGrid.Visibility = Visibility.Collapsed;
            };

            storyboard.Begin();
        }

        private void OnMainWindowSourceInitialized(object sender, EventArgs e) {
            dataContext = DataContext as IMainWindowPresenter;
            if (dataContext == null) return;

            // register callbacks
            dataContext.IsAuthenticated += (o, args) => context.Post(state => HideLoginScreen(), null);
            dataContext.EditPresentation += (o, pres) => context.Post(state => EditPresentation(pres), null);
            dataContext.PresentationList += (o, pres) => context.Post(state => ShowSelectPresentation(pres), null);
            dataContext.PresentationSaved += (o, pres) => context.Post(state => HideEditPresentation(), null);
            dataContext.PresentationDeleted += (o, pres) => context.Post(state => HideEditPresentation(), null);
            dataContext.ShowPresentation += (o, pres) => context.Post(state => ShowPresentation(pres), null);
            dataContext.CancelStartPresentation += (o, args) => context.Post(state => CancelShowPresentation(), null);
            dataContext.GotPresentationSlidesCount += (o, count) => context.Post(state => GotPresentationSlidesCount(count), null);
            dataContext.GotPresentationSlide += (o, args) => context.Post(state => GotPresentationSlide(), null);
        }

        private void HideEditPresentation() {
            CancelShowPresentation();
            EditPresentationView.Visibility = Visibility.Hidden;
        }

        private void EditPresentation(Presentation pres) {
            currentPresentation = pres;
            EditPresentationSlides.ItemsSource = null;

            if (NewPresentationGrid.Visibility == Visibility.Visible) {
                HideNewPresentationScreen(pres != null);
            }
            if (SelectPresentationView.Visibility == Visibility.Visible) {
                HideSelectPresentation();
            }

            if (pres != null) {
                EditPresentationView.Visibility = Visibility.Visible;
                EditPresentationName.Content = pres.Name;
                ImportSlides();
            }

        }

        private void ImportSlides(int slideIndex = -1) {
            ImportSlidesTitle.Text = "Importing Slides";
            ImportSlidesProgressBar.Maximum = 1;
            ImportSlidesProgressBar.Value = 0;
            OverlayRectangle.Opacity = 0.6;
            OverlayRectangle.Visibility = Visibility.Visible;
            ImportSlidesGrid.Visibility = Visibility.Visible;

            var importedSlides = new List<Slide>();
            var slides = new ObservableCollection<SlidePreview>();

            new Thread(() => {
                var fileName = droppedFileName;
                droppedFileName = null;
                if (currentPresentation == null) return;

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
                            context.Post(state => {
                                ImportSlidesProgressBar.Maximum = ((SlidesImporterStatus)state).TotalSlides;
                                ImportSlidesProgressBar.Value = ((SlidesImporterStatus)state).CurrentSlideIndex;
                            }, slideData);
                            importedSlides.Insert(slideIndex != -1 ? (slideIndex-1) : slideNumber-1, new Slide {
                                ImageData = slideData.CurrentSlideData,
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

                context.Post(state => {
                    foreach (var slide in importedSlides) {
                        slides.Add(SlidePreview.CreateFromSlide(slide, currentPresentation.Id));
                    }
                    slides.Add(SlidePreview.CreateAddNewSlide(currentPresentation.Id));
                    currentPresentation.Slides = importedSlides;
                    EditPresentationSlides.ItemsSource = slides;
                    OverlayRectangle.Visibility = Visibility.Hidden;
                    ImportSlidesGrid.Visibility = Visibility.Hidden;
                }, null);
            }).Start();
        }

        
        private void GotPresentationSlidesCount(int count) {
            ImportSlidesProgressBar.Maximum = count;
        }
        
        private void GotPresentationSlide() {
            ImportSlidesProgressBar.Value ++;
        }

        private void CancelShowPresentation() {
            OverlayRectangle.Visibility = Visibility.Hidden;
            ImportSlidesGrid.Visibility = Visibility.Hidden;
        }

        private void OnCancelNewPresentationClick(object sender, RoutedEventArgs e) {
            HideNewPresentationScreen();
        }
        
        private void ShowSelectPresentation(IEnumerable<PresentationPreview> presentationList) {
            CancelShowPresentation();
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
            if (e.LeftButton != MouseButtonState.Pressed || e.ClickCount != 2) return;
            var slidePreview = SelectPresentationList.SelectedItem as SlidePreview;
            if (slidePreview == null || slidePreview.PresentationId == null) return;

            ImportSlidesProgressBar.Maximum = 1;
            ImportSlidesProgressBar.Value = 0;
            ImportSlidesTitle.Text = "Loading Slides";
            OverlayRectangle.Opacity = 0.6;
            OverlayRectangle.Visibility = Visibility.Visible;
            ImportSlidesGrid.Visibility = Visibility.Visible;

            dataContext.StartPresentation(slidePreview);
        }

        private void ShowPresentation(Presentation pres) {
            CancelShowPresentation();
            new PresentationWindow(dataContext) {
                DataContext = pres
            }.ShowDialog();
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

        private void OnSelectPresentationListSelectionChanged(object sender, SelectionChangedEventArgs e) {
            var slidePreview = SelectPresentationList.SelectedItem as SlidePreview;
            EditPresentationButton.IsEnabled = slidePreview != null && slidePreview.PresentationId != null;
        }

        private void OnEditPresentationClick(object sender, RoutedEventArgs e) {
            var slidePreview = SelectPresentationList.SelectedItem as SlidePreview;
            if (slidePreview == null || slidePreview.PresentationId == null) return;

            ImportSlidesTitle.Text = "Loading Slides";
            ImportSlidesProgressBar.Maximum = 1;
            ImportSlidesProgressBar.Value = 0;
            OverlayRectangle.Opacity = 0.6;
            OverlayRectangle.Visibility = Visibility.Visible;
            ImportSlidesGrid.Visibility = Visibility.Visible;

            dataContext.ChangePresentation(slidePreview);
        }

        private void OnSavePresentationClick(object sender, RoutedEventArgs e) {
            ImportSlidesProgressBar.Maximum = 1;
            ImportSlidesProgressBar.Value = 0;
            ImportSlidesTitle.Text = "Saving Slides";
            OverlayRectangle.Opacity = 0.6;
            OverlayRectangle.Visibility = Visibility.Visible;
            ImportSlidesGrid.Visibility = Visibility.Visible;

            dataContext.SavePresentation();
        }

        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
            Environment.Exit(0);
        }

        private void OnGetPresentationsButtonClick(object sender, RoutedEventArgs e) {
            ImportSlidesTitle.Text = "Loading Presentations";
            ImportSlidesProgressBar.Maximum = 1;
            ImportSlidesProgressBar.Value = 0;
            OverlayRectangle.Opacity = 0.6;
            OverlayRectangle.Visibility = Visibility.Visible;
            ImportSlidesGrid.Visibility = Visibility.Visible;

            dataContext.GetPresentations();
        }
    }
}