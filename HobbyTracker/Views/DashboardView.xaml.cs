using HobbyTracker.ViewModels;
using HobbyTracker.Views.Dashboard;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using HobbyTracker.Services;

namespace HobbyTracker.Views
{
    public partial class DashboardView : System.Windows.Controls.UserControl
    {
        private readonly DashboardViewModel _viewModel;
        
        // Sekme UserControl'leri
        private DashboardOyunlarView? _oyunlarView;
        private DashboardKitaplarView? _kitaplarView;
        private DashboardSinemaView? _sinemaView;

        public DashboardView()
        {
            InitializeComponent();
            _viewModel = new DashboardViewModel();
            this.DataContext = _viewModel;
            this.Loaded += DashboardView_Loaded;
        }

        private async void DashboardView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // Kullanıcı adını göster
            if (WelcomeText != null && !string.IsNullOrEmpty(Models.UserSession.CurrentUserName))
            {
                WelcomeText.Text = $"Tekrar Hoş Geldin, {Models.UserSession.CurrentUserName}!";
            }

            await _viewModel.LoadStatsAsync();

            // Boş durum kontrolü
            UpdateEmptyStates();

            // Şık bar grafiğini oluştur
            CreateBarChart();

            // İlk sekmenin rengini ayarla
            if (TabGenelBakis != null)
            {
                UpdateTabColors(TabGenelBakis);
            }
        }

        public void SelectTab(string tabName)
        {
            var targetTab = this.FindName(tabName) as System.Windows.Controls.RadioButton;
            if (targetTab != null)
            {
                targetTab.IsChecked = true;
                UpdateTabColors(targetTab);
                LoadTabContent(tabName);
            }
        }

        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.RadioButton radioButton)
            {
                string tabName = radioButton.Name;
                
                // ViewModel'e sekme değişikliğini bildir
                if (_viewModel != null)
                {
                    _viewModel.SelectedTab = tabName.Replace("Tab", "");
                }

                // Seçilen sekmenin rengini al ve uygula
                UpdateTabColors(radioButton);

                // Sekmeye göre içerik yükle
                LoadTabContent(tabName);

                System.Diagnostics.Debug.WriteLine($"Sekme değişti: {tabName}");
            }
        }

        private void LoadTabContent(string tabName)
        {
            // TabContent ContentControl kontrol et
            if (TabContent == null) return;

            switch (tabName)
            {
                case "TabGenelBakis":
                    // Genel Bakış: Mevcut içeriği göster, TabContent'i gizle
                    TabContent.Content = null;
                    TabContent.Visibility = Visibility.Collapsed;
                    ShowMainContent(true);
                    break;

                case "TabOyunlar":
                    // Oyunlar sekmesi: UserControl yükle
                    if (_oyunlarView == null)
                    {
                        _oyunlarView = new DashboardOyunlarView();
                    }
                    else
                    {
                        _oyunlarView.RefreshData();
                    }
                    TabContent.Content = _oyunlarView;
                    TabContent.Visibility = Visibility.Visible;
                    ShowMainContent(false);
                    break;

                case "TabKitaplar":
                    // Kitaplar sekmesi
                    if (_kitaplarView == null)
                    {
                        _kitaplarView = new DashboardKitaplarView();
                    }
                    else
                    {
                        _kitaplarView.RefreshData();
                    }
                    TabContent.Content = _kitaplarView;
                    TabContent.Visibility = Visibility.Visible;
                    ShowMainContent(false);
                    break;

                case "TabSinema":
                    // Sinema sekmesi: DashboardSinemaView yükle
                    if (_sinemaView == null)
                    {
                        _sinemaView = new DashboardSinemaView();
                    }
                    else
                    {
                        _sinemaView.LoadDataAsync();
                    }
                    TabContent.Content = _sinemaView;
                    TabContent.Visibility = Visibility.Visible;
                    ShowMainContent(false);
                    break;

                case "TabVitrin":
                    // Vitrin sekmesi (TODO: UserControl eklenecek)
                    TabContent.Content = CreatePlaceholder("✨", "Vitrin", "Öne çıkan içerikler yakında eklenecek...", "#eab308");
                    TabContent.Visibility = Visibility.Visible;
                    ShowMainContent(false);
                    break;
            }
        }

        private void ShowMainContent(bool show)
        {
            // Mevcut içeriği (Row 1,2,3) göster/gizle
            var visibility = show ? Visibility.Visible : Visibility.Collapsed;
            
            // İstatistik kartları ve diğer içerikleri bul ve visibility değiştir
            if (RootGrid != null)
            {
                foreach (UIElement child in RootGrid.Children)
                {
                    if (child is Grid grid)
                    {
                        int row = Grid.GetRow(grid);
                        // Row 0 header, Row 1-3 içerik
                        if (row >= 1)
                        {
                            grid.Visibility = visibility;
                        }
                    }
                }
            }
        }

        private Border CreatePlaceholder(string emoji, string title, string description, string colorHex)
        {
            var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex);
            
            return new Border
            {
                Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1e293b")),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(60),
                Margin = new Thickness(0, 0, 0, 25),
                Child = new StackPanel
                {
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    Children =
                    {
                        new TextBlock { Text = emoji, FontSize = 60, HorizontalAlignment = System.Windows.HorizontalAlignment.Center },
                        new TextBlock { Text = title, Foreground = new SolidColorBrush(color), FontSize = 24, FontWeight = FontWeights.Bold, HorizontalAlignment = System.Windows.HorizontalAlignment.Center, Margin = new Thickness(0,20,0,10) },
                        new TextBlock { Text = description, Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#94a3b8")), FontSize = 14, HorizontalAlignment = System.Windows.HorizontalAlignment.Center }
                    }
                }
            };
        }

        private void UpdateTabColors(System.Windows.Controls.RadioButton selectedTab)
        {
            // Seçilen sekmenin Tag'ından rengi al
            if (selectedTab?.Tag == null) return;

            string colorHex = selectedTab.Tag.ToString();
            var activeColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex);
            var activeBrush = new SolidColorBrush(activeColor);

            // Tüm sekmeleri dolaş ve renklerini ayarla
            var tabs = new[] { TabGenelBakis, TabOyunlar, TabKitaplar, TabSinema, TabVitrin };
            
            foreach (var tab in tabs)
            {
                if (tab == null) continue;

                if (tab == selectedTab)
                {
                    // Seçili sekme: kendi renginde
                    tab.Background = activeBrush;
                }
                else
                {
                    // Seçili olmayan sekmeler: şeffaf
                    tab.Background = System.Windows.Media.Brushes.Transparent;
                }
            }
        }

        private void UpdateEmptyStates()
        {
            // Devam edilecekler boş durumu
            if (_viewModel.ContinueItems == null || _viewModel.ContinueItems.Count == 0)
            {
                if (ContinueItemsControl != null) ContinueItemsControl.Visibility = Visibility.Collapsed;
                if (EmptyContinueState != null) EmptyContinueState.Visibility = Visibility.Visible;
            }
            else
            {
                if (ContinueItemsControl != null) ContinueItemsControl.Visibility = Visibility.Visible;
                if (EmptyContinueState != null) EmptyContinueState.Visibility = Visibility.Collapsed;
            }

            // Yeni eklenenler boş durumu
            if (_viewModel.RecentItems == null || _viewModel.RecentItems.Count == 0)
            {
                if (RecentItemsControl != null) RecentItemsControl.Visibility = Visibility.Collapsed;
                if (EmptyRecentState != null) EmptyRecentState.Visibility = Visibility.Visible;
            }
            else
            {
                if (RecentItemsControl != null) RecentItemsControl.Visibility = Visibility.Visible;
                if (EmptyRecentState != null) EmptyRecentState.Visibility = Visibility.Collapsed;
            }
        }

        private void CreateBarChart()
        {
            // Grafik verilerini al
            var values = _viewModel.ChartValues ?? new List<int> { 0, 0, 0, 0, 0, 0, 0 };
            var labels = new[] { "Pzt", "Sal", "Çar", "Per", "Cum", "Cmt", "Paz" };

            // Haftalık aktivite verisi varsa gün isimlerini güncelle
            if (_viewModel.Stats?.WeeklyActivity != null && _viewModel.Stats.WeeklyActivity.Count == 7)
            {
                labels = new string[7];
                values = new List<int>();
                for (int i = 0; i < 7; i++)
                {
                    labels[i] = _viewModel.Stats.WeeklyActivity[i].DayName;
                    values.Add(_viewModel.Stats.WeeklyActivity[i].ActivityCount);
                }
            }

            // DevExpress ChartControl'a veri ekle
            if (ActivitySeries != null)
            {
                ActivitySeries.Points.Clear();
                
                for (int i = 0; i < labels.Length && i < values.Count; i++)
                {
                    ActivitySeries.Points.Add(new DevExpress.Xpf.Charts.SeriesPoint(labels[i], values[i]));
                }
            }
        }

        private async void ContinueItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is Models.RecentItem item)
            {
                var firebaseService = new SFirebase();
                
                if (item.Category == "Oyun")
                {
                    System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                    var game = await firebaseService.GetGameAsync(item.Id);
                    System.Windows.Input.Mouse.OverrideCursor = null;

                    if (game != null)
                    {
                        var editWindow = new EditGameWindow(game);
                        editWindow.ShowDialog();
                        // Dönüşte listeyi güncelle
                        if (_viewModel != null) await _viewModel.LoadStatsAsync();
                    }
                }
                else if (item.Category == "Kitap")
                {
                    System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                    var book = await firebaseService.GetBookAsync(item.Id);
                    System.Windows.Input.Mouse.OverrideCursor = null;

                    if (book != null)
                    {
                        var editWindow = new EditBookWindow(book);
                        editWindow.ShowDialog();
                        if (_viewModel != null) await _viewModel.LoadStatsAsync();
                    }
                }
                else if (item.Category == "Dizi")
                {
                    System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                    var series = await firebaseService.GetSeriesAsync(item.Id);
                    System.Windows.Input.Mouse.OverrideCursor = null;

                    if (series != null)
                    {
                        var mainWindow = Window.GetWindow(this) as MainWindow;
                        if (mainWindow != null)
                        {
                            mainWindow.NavigateToSeriesDetail(series);
                        }
                    }
                }
                else if (item.Category == "Film")
                {
                    System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                    var movie = await firebaseService.GetMovieAsync(item.Id);
                    System.Windows.Input.Mouse.OverrideCursor = null;

                    if (movie != null)
                    {
                        var mainWindow = Window.GetWindow(this) as MainWindow;
                        if (mainWindow != null)
                        {
                            mainWindow.NavigateToMovieDetail(movie);
                        }
                    }
                }
            }
        }
    }
}