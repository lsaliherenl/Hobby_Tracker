using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using DevExpress.Xpf.Charts;
using HobbyTracker.Services;
using HobbyTracker.Models;

namespace HobbyTracker.Views.Dashboard
{
    /// <summary>
    /// Dashboard Oyunlar sekmesi - oyun istatistikleri ve grafikler
    /// </summary>
    public partial class DashboardOyunlarView : System.Windows.Controls.UserControl
    {
        // Dependency Properties for Binding
        public static readonly DependencyProperty FavoriteGamesProperty =
            DependencyProperty.Register("FavoriteGames", typeof(System.Collections.ObjectModel.ObservableCollection<Game>), typeof(DashboardOyunlarView), new PropertyMetadata(null));

        public System.Collections.ObjectModel.ObservableCollection<Game> FavoriteGames
        {
            get { return (System.Collections.ObjectModel.ObservableCollection<Game>)GetValue(FavoriteGamesProperty); }
            set { SetValue(FavoriteGamesProperty, value); }
        }

        public static readonly DependencyProperty HasNoFavoritesProperty =
            DependencyProperty.Register("HasNoFavorites", typeof(bool), typeof(DashboardOyunlarView), new PropertyMetadata(true));

        public bool HasNoFavorites
        {
            get { return (bool)GetValue(HasNoFavoritesProperty); }
            set { SetValue(HasNoFavoritesProperty, value); }
        }

        public static readonly DependencyProperty LongestPlayedGameProperty =
            DependencyProperty.Register("LongestPlayedGame", typeof(Game), typeof(DashboardOyunlarView), new PropertyMetadata(null));

        public Game LongestPlayedGame
        {
            get { return (Game)GetValue(LongestPlayedGameProperty); }
            set { SetValue(LongestPlayedGameProperty, value); }
        }

        public static readonly DependencyProperty CurrentFavoriteGameProperty =
            DependencyProperty.Register("CurrentFavoriteGame", typeof(Game), typeof(DashboardOyunlarView), new PropertyMetadata(null));

        public Game CurrentFavoriteGame
        {
            get { return (Game)GetValue(CurrentFavoriteGameProperty); }
            set { SetValue(CurrentFavoriteGameProperty, value); }
        }

        private int _currentFavoriteIndex = 0;

        private List<Game> _games;
        private List<Game> _favoriteGamesList; // Navigasyon için liste referansı

        public DashboardOyunlarView()
        {
            InitializeComponent();
            this.DataContext = this; // Binding için set edildi
            Loaded += DashboardOyunlarView_Loaded;
        }

        private async void DashboardOyunlarView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadGameDataAsync();
        }

        public async Task LoadGameDataAsync()
        {
            try
            {
                var firebase = new SFirebase();
                _games = await firebase.GetGamesAsync();

                if (_games == null || _games.Count == 0)
                {
                    ShowEmptyState();
                    FavoriteGames = new System.Collections.ObjectModel.ObservableCollection<Game>();
                    HasNoFavorites = true;
                    LongestPlayedGame = null;
                    CurrentFavoriteGame = null;
                    return;
                }

                UpdateStatistics();
                LoadRecentGames();
                LoadGenreChart();
                LoadPlatformStats();

                // Favori Oyunları Yükle
                var favs = _games.Where(g => g.IsFavorite).OrderByDescending(g => g.UserRating).ToList();
                FavoriteGames = new System.Collections.ObjectModel.ObservableCollection<Game>(favs);
                _favoriteGamesList = favs;
                HasNoFavorites = favs.Count == 0;

                // İlk favori oyunu seç
                if (favs.Count > 0)
                {
                    _currentFavoriteIndex = 0;
                    CurrentFavoriteGame = favs[0];
                }
                else
                {
                    CurrentFavoriteGame = null;
                }

                // En Uzun Süre Oynanan Oyunu Bul
                LongestPlayedGame = _games.OrderByDescending(g => g.PlayTime).FirstOrDefault();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Oyun verileri yüklenirken hata: {ex.Message}");
            }
        }
        
        private void NextFavorite_Click(object sender, RoutedEventArgs e)
        {
            if (_favoriteGamesList == null || _favoriteGamesList.Count == 0) return;

            _currentFavoriteIndex++;
            if (_currentFavoriteIndex >= _favoriteGamesList.Count)
            {
                _currentFavoriteIndex = 0; // Başa dön
            }
            CurrentFavoriteGame = _favoriteGamesList[_currentFavoriteIndex];
        }

        private void PrevFavorite_Click(object sender, RoutedEventArgs e)
        {
             if (_favoriteGamesList == null || _favoriteGamesList.Count == 0) return;

            _currentFavoriteIndex--;
            if (_currentFavoriteIndex < 0)
            {
                _currentFavoriteIndex = _favoriteGamesList.Count - 1; // Sona dön
            }
            CurrentFavoriteGame = _favoriteGamesList[_currentFavoriteIndex];
        }

        private void CurrentFavorite_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
             if (CurrentFavoriteGame != null)
            {
                var editWindow = new EditGameWindow(CurrentFavoriteGame);
                if (editWindow.ShowDialog() == true)
                {
                    RefreshData();
                }
            }
        }

        
        // ... (Diğer metodlar aynı kalıyor)

        private void OpenGameDetails_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Game game)
            {
                var editWindow = new EditGameWindow(game);
                if (editWindow.ShowDialog() == true)
                {
                    RefreshData(); // Değişiklik varsa verileri yenile
                }
            }
        }

        private void LongestGame_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (LongestPlayedGame != null)
            {
                var editWindow = new EditGameWindow(LongestPlayedGame);
                if (editWindow.ShowDialog() == true)
                {
                    RefreshData();
                }
            }
        }


        private void UpdateStatistics()
        {
            if (_games == null) return;

            int total = _games.Count;
            int finished = _games.Count(g => g.Status == "Tamamlandı");
            int playing = _games.Count(g => g.Status == "Oynuyor");
            
            // Toplam oynama süresi (İstek Listesi hariç)
            int totalPlayTime = _games.Where(g => g.Status != "İstek Listesi").Sum(g => g.PlayTime);
            
            // Ortalama puan (İstek Listesi hariç ve 0 olmayan puanlar - GamesView ile aynı mantık)
            var ratedGames = _games.Where(g => g.Status != "İstek Listesi" && g.UserRating > 0).ToList();
            double avgRating = ratedGames.Count > 0 ? ratedGames.Average(g => g.UserRating) : 0;

            // UI güncelle
            TotalGamesText.Text = total.ToString();
            FinishedGamesText.Text = finished.ToString();
            PlayingGamesText.Text = playing.ToString();
            AvgRatingText.Text = avgRating.ToString("F1");
            TotalPlayTimeText.Text = totalPlayTime.ToString();

            // Tamamlanma yüzdesi
            double completionRate = total > 0 ? (finished * 100.0 / total) : 0;
            CompletionRateText.Text = $"%{completionRate:F0} tamamlanma";
        }

        private void LoadRecentGames()
        {
            if (_games == null || _games.Count == 0)
            {
                EmptyGamesState.Visibility = Visibility.Visible;
                RecentGamesControl.Visibility = Visibility.Collapsed;
                return;
            }

            // Son eklenen 4 oyunu göster
            var recentGames = _games
                .OrderByDescending(g => g.AddedDate)
                .Take(4)
                .ToList();

            RecentGamesControl.ItemsSource = recentGames;
            RecentGamesControl.Visibility = Visibility.Visible;
            EmptyGamesState.Visibility = Visibility.Collapsed;
        }

        // Grafik Renk Paleti (Canlı ve Ayırt Edici Renkler)
        private static readonly string[] _purplePalette = new[]
        {
            "#A855F7", // Vivid Purple
            "#06B6D4", // Cyan
            "#F43F5E", // Rose
            "#EAB308", // Yellow
            "#3B82F6", // Blue 
            "#10B981"  // Emerald
        };

        private void LoadGenreChart()
        {
            if (_games == null || GenreSeries == null) return;

            // Türlere göre grupla
            var genreGroups = _games
                .SelectMany(g => (g.Genres ?? "Diğer").Split(','))
                .Select(genre => genre.Trim())
                .Where(genre => !string.IsNullOrEmpty(genre))
                .GroupBy(genre => genre)
                .Select(g => new { Genre = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(6)
                .ToList();

            GenreSeries.Points.Clear();
            var legendItems = new List<GenreLegendItem>();
            
            for (int i = 0; i < genreGroups.Count; i++)
            {
                var genre = genreGroups[i];
                var colorHex = _purplePalette[i % _purplePalette.Length];
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex);
                
                var point = new SeriesPoint(genre.Genre, genre.Count);
                GenreSeries.Points.Add(point);
                
                legendItems.Add(new GenreLegendItem 
                { 
                    Name = genre.Genre, 
                    Color = color 
                });
            }

            // Manuel legend'ı güncelle
            if (GenreLegend != null)
            {
                GenreLegend.ItemsSource = legendItems;
            }

            // Grafik renk paletini ayarla (Legend ile aynı renkleri kullanması için)
            if (GenreChart != null)
            {
                var palette = new DevExpress.Xpf.Charts.CustomPalette();
                foreach (var hex in _purplePalette)
                {
                    palette.Colors.Add((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
                }
                GenreChart.Palette = palette;
            }
        }

        private void LoadPlatformStats()
        {
            if (_games == null || PlatformSeries == null) return;

            // Platformlara göre grupla
            var platformStats = _games
                .GroupBy(g => g.Platform ?? "Diğer")
                .Select(g => new { Platform = g.Key, Count = g.Count() })
                .OrderByDescending(p => p.Count)
                .Take(5)
                .ToList();

            PlatformSeries.Points.Clear();
            
            // Renk paletini ayarla
            if (PlatformChart != null)
            {
                var palette = new DevExpress.Xpf.Charts.CustomPalette();
                foreach (var hex in _purplePalette)
                {
                    palette.Colors.Add((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
                }
                PlatformChart.Palette = palette;
            }

            foreach (var stat in platformStats)
            {
                PlatformSeries.Points.Add(new SeriesPoint(stat.Platform, stat.Count));
            }
        }

        private void ShowEmptyState()
        {
            TotalGamesText.Text = "0";
            FinishedGamesText.Text = "0";
            PlayingGamesText.Text = "0";
            AvgRatingText.Text = "0";
            TotalPlayTimeText.Text = "0";
            CompletionRateText.Text = "%0 tamamlanma";

            EmptyGamesState.Visibility = Visibility.Visible;
            RecentGamesControl.Visibility = Visibility.Collapsed;
        }

        private void ViewAllGames_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow; 
            if (mainWindow != null) 
            {
                mainWindow.SwitchToGames();
            }
        }

        /// <summary>
        /// Verileri yenile
        /// </summary>
        public async void RefreshData()
        {
            await LoadGameDataAsync();
        }
    }

    /// <summary>
    /// Platform istatistik modeli
    /// </summary>
    public class PlatformStat
    {
        public string PlatformName { get; set; }
        public int Count { get; set; }
    }

    public class GenreLegendItem
    {
        public string Name { get; set; }
        public System.Windows.Media.Color Color { get; set; }
    }
}
