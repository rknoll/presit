﻿using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace PresIt.Windows {
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow {
        private readonly SynchronizationContext context;

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
            if (files.Length != 1) return;
            var file = files[0];
        }

        private void OnNewPresentationClick(object sender, RoutedEventArgs e) {
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

        private void OnMainWindowSourceInitialized(object sender, EventArgs e) {
            // get the data context and execute the shuffle command if possible
            var dataContext = DataContext as IMainWindowPresenter;
            if (dataContext == null) return;
            dataContext.IsAuthenticated += (o, args) => context.Post(state => {
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
            }, null);
        }
    }
}