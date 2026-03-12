using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace HobbyTracker.Helpers
{
    public enum ToastType
    {
        Success,
        Error,
        Warning,
        Info
    }

    public static class ToastNotification
    {
        private static Border _currentToast;
        private static DispatcherTimer _autoCloseTimer;

        /// <summary>
        /// Shows a toast notification at the bottom-right of the main window.
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <param name="type">Type of toast (Success, Error, Warning, Info)</param>
        /// <param name="durationSeconds">Auto-close duration in seconds (default 5)</param>
        public static void Show(string message, ToastType type = ToastType.Success, int durationSeconds = 5)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    var mainWindow = System.Windows.Application.Current.MainWindow;
                    if (mainWindow == null) return;

                    // Find the main content grid
                    var mainGrid = FindMainGrid(mainWindow);
                    if (mainGrid == null) 
                    {
                        System.Diagnostics.Debug.WriteLine("ToastNotification: MainGrid bulunamadı!");
                        return;
                    }

                    // Remove existing toast if any
                    if (_currentToast != null && mainGrid.Children.Contains(_currentToast))
                    {
                        mainGrid.Children.Remove(_currentToast);
                    }

                    // Get colors based on type
                    var (bgColor, iconText, accentColor) = GetToastStyle(type);

                    // Create toast container
                    var toast = new Border
                    {
                        Name = "ToastNotification",
                        Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(bgColor)),
                        CornerRadius = new CornerRadius(12),
                        Padding = new Thickness(16, 12, 16, 12),
                        Margin = new Thickness(0, 0, 30, 30),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                        VerticalAlignment = System.Windows.VerticalAlignment.Bottom,
                        MaxWidth = 400,
                        MinWidth = 280,
                        Effect = new System.Windows.Media.Effects.DropShadowEffect
                        {
                            Color = System.Windows.Media.Colors.Black,
                            BlurRadius = 20,
                            ShadowDepth = 5,
                            Opacity = 0.5
                        },
                        RenderTransformOrigin = new System.Windows.Point(1, 1),
                        RenderTransform = new TranslateTransform(0, 100),
                        Opacity = 0
                    };

                    // Create content
                    var stackPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };

                    // Icon
                    var icon = new TextBlock
                    {
                        Text = iconText,
                        FontSize = 20,
                        Foreground = System.Windows.Media.Brushes.White,
                        VerticalAlignment = System.Windows.VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 12, 0)
                    };

                    // Message
                    var messageBlock = new TextBlock
                    {
                        Text = message,
                        Foreground = System.Windows.Media.Brushes.White,
                        FontSize = 14,
                        FontWeight = FontWeights.Medium,
                        VerticalAlignment = System.Windows.VerticalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap
                    };

                    // Accent bar
                    var accentBar = new Border
                    {
                        Width = 4,
                        Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(accentColor)),
                        CornerRadius = new CornerRadius(2),
                        Margin = new Thickness(0, 0, 12, 0)
                    };

                    stackPanel.Children.Add(accentBar);
                    stackPanel.Children.Add(icon);
                    stackPanel.Children.Add(messageBlock);
                    toast.Child = stackPanel;

                    // Add to grid with highest z-index - span all columns
                    if (mainGrid.ColumnDefinitions.Count > 1)
                    {
                        Grid.SetColumnSpan(toast, mainGrid.ColumnDefinitions.Count);
                    }
                    if (mainGrid.RowDefinitions.Count > 1)
                    {
                        Grid.SetRowSpan(toast, mainGrid.RowDefinitions.Count);
                    }
                    System.Windows.Controls.Panel.SetZIndex(toast, 9999);
                    mainGrid.Children.Add(toast);
                    _currentToast = toast;

                    // Animate in
                    var translateAnimation = new DoubleAnimation(100, 0, TimeSpan.FromMilliseconds(300))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    var opacityAnimation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));

                    ((TranslateTransform)toast.RenderTransform).BeginAnimation(TranslateTransform.YProperty, translateAnimation);
                    toast.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);

                    // Auto-close timer
                    _autoCloseTimer?.Stop();
                    _autoCloseTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(durationSeconds) };
                    _autoCloseTimer.Tick += (s, e) =>
                    {
                        _autoCloseTimer.Stop();
                        HideToast(mainGrid);
                    };
                    _autoCloseTimer.Start();

                    // Click to dismiss
                    toast.MouseDown += (s, e) =>
                    {
                        _autoCloseTimer?.Stop();
                        HideToast(mainGrid);
                    };
                    
                    System.Diagnostics.Debug.WriteLine($"ToastNotification: '{message}' gösteriliyor");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ToastNotification hatası: {ex.Message}");
                }
            });
        }

        private static void HideToast(Grid mainGrid)
        {
            if (_currentToast == null) return;

            var translateAnimation = new DoubleAnimation(0, 100, TimeSpan.FromMilliseconds(200))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            var opacityAnimation = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));

            opacityAnimation.Completed += (s, e) =>
            {
                if (mainGrid.Children.Contains(_currentToast))
                {
                    mainGrid.Children.Remove(_currentToast);
                }
                _currentToast = null;
            };

            ((TranslateTransform)_currentToast.RenderTransform).BeginAnimation(TranslateTransform.YProperty, translateAnimation);
            _currentToast.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);
        }

        private static Grid FindMainGrid(Window window)
        {
            // Method 1: Direct content check
            if (window.Content is Grid grid)
            {
                System.Diagnostics.Debug.WriteLine("ToastNotification: Grid doğrudan Content'te bulundu");
                return grid;
            }

            // Method 2: Search in visual tree for the first Grid
            Grid foundGrid = FindVisualChild<Grid>(window);
            if (foundGrid != null)
            {
                System.Diagnostics.Debug.WriteLine("ToastNotification: Grid VisualTree'de bulundu");
                return foundGrid;
            }

            // Method 3: For DevExpress ThemedWindow, try to get content differently
            try
            {
                var contentProperty = window.GetType().GetProperty("Content");
                if (contentProperty != null)
                {
                    var content = contentProperty.GetValue(window);
                    if (content is Grid g)
                    {
                        System.Diagnostics.Debug.WriteLine("ToastNotification: Grid reflection ile bulundu");
                        return g;
                    }
                }
            }
            catch { }

            return null;
        }
        
        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;
                    
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }

        private static (string bgColor, string icon, string accentColor) GetToastStyle(ToastType type)
        {
            return type switch
            {
                ToastType.Success => ("#1a2e1a", "✓", "#22c55e"),
                ToastType.Error => ("#2e1a1a", "✕", "#ef4444"),
                ToastType.Warning => ("#2e2a1a", "⚠", "#f59e0b"),
                ToastType.Info => ("#1a1a2e", "ℹ", "#3b82f6"),
                _ => ("#1a2e1a", "✓", "#22c55e")
            };
        }
    }
}
