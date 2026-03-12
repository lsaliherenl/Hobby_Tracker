using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using HobbyTracker.Services;
using HobbyTracker.Models;

namespace HobbyTracker.Views.Dashboard
{
    public partial class DashboardSinemaView : System.Windows.Controls.UserControl
    {
        private List<Movie> _movies;
        private List<Series> _series;
        private List<Movie> _favoriteMovies;
        private List<Series> _favoriteSeries;
        private int _currentMovieIndex = 0;
        private int _currentSeriesIndex = 0;

        public DashboardSinemaView()
        {
            InitializeComponent();
            Loaded += DashboardSinemaView_Loaded;
        }

        private async void DashboardSinemaView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            try
            {
                var firebase = new SFirebase();
                _movies = await firebase.GetMoviesAsync();
                _series = await firebase.GetSeriesAsync();

                if ((_movies == null || _movies.Count == 0) && (_series == null || _series.Count == 0))
                {
                    ShowEmptyState();
                    return;
                }

                _movies = _movies ?? new List<Movie>();
                _series = _series ?? new List<Series>();

                UpdateStatistics();
                LoadVakitSavasi();
                LoadRecentItems();
                LoadMonthlyWatchTime();
                LoadFavoriteDirectors();
                LoadDecadeAnalysis();
                LoadFavoriteMovies();
                LoadFavoriteSeries();
                LoadLongestContent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sinema verileri yüklenirken hata: {ex.Message}");
            }
        }

        private void UpdateStatistics()
        {
            // Film istatistikleri
            int totalMovies = _movies.Count;
            int completedMovies = _movies.Count(m => m.UserStatus == WatchStatus.Completed);
            double movieCompletionRate = totalMovies > 0 ? (completedMovies * 100.0 / totalMovies) : 0;

            TotalMoviesText.Text = totalMovies.ToString();
            MovieCompletionText.Text = $"{completedMovies} tamamlandı (%{movieCompletionRate:F0})";

            // Dizi istatistikleri
            int totalSeries = _series.Count;
            int completedSeries = _series.Count(s => s.UserStatus == WatchStatus.Completed);
            double seriesCompletionRate = totalSeries > 0 ? (completedSeries * 100.0 / totalSeries) : 0;

            TotalSeriesText.Text = totalSeries.ToString();
            SeriesCompletionText.Text = $"{completedSeries} tamamlandı (%{seriesCompletionRate:F0})";

            // Toplam süre hesaplama
            int movieMinutes = _movies.Where(m => m.UserStatus == WatchStatus.Completed || m.UserStatus == WatchStatus.InProgress)
                                      .Sum(m => m.WatchedMinutes > 0 ? m.WatchedMinutes : m.DurationMinutes);
            int seriesMinutes = _series.Sum(s => s.TotalWatchedMinutes);
            int totalMinutes = movieMinutes + seriesMinutes;
            int totalHours = totalMinutes / 60;

            TotalWatchTimeText.Text = totalHours.ToString();
            WatchTimeBreakdownText.Text = $"{movieMinutes / 60}s film, {seriesMinutes / 60}s dizi";

            // Devam eden
            int inProgressMovies = _movies.Count(m => m.UserStatus == WatchStatus.InProgress);
            int inProgressSeries = _series.Count(s => s.UserStatus == WatchStatus.InProgress);
            int totalInProgress = inProgressMovies + inProgressSeries;

            InProgressCountText.Text = totalInProgress.ToString();
            InProgressBreakdownText.Text = $"{inProgressMovies} film, {inProgressSeries} dizi";

            // Ortalama puan
            var ratedMovies = _movies.Where(m => m.UserRating.HasValue && m.UserRating > 0).ToList();
            var ratedSeries = _series.Where(s => s.UserRating > 0).ToList();
            int totalRated = ratedMovies.Count + ratedSeries.Count;

            double avgRating = 0;
            if (totalRated > 0)
            {
                double movieRatingSum = ratedMovies.Sum(m => m.UserRating ?? 0);
                double seriesRatingSum = ratedSeries.Sum(s => s.UserRating);
                avgRating = (movieRatingSum + seriesRatingSum) / totalRated;
            }

            AvgRatingText.Text = avgRating.ToString("F1");
            RatingCountText.Text = $"{totalRated} değerlendirme";
        }

        private void LoadVakitSavasi()
        {
            int movieMinutes = _movies.Where(m => m.UserStatus == WatchStatus.Completed || m.UserStatus == WatchStatus.InProgress)
                                      .Sum(m => m.WatchedMinutes > 0 ? m.WatchedMinutes : m.DurationMinutes);
            int seriesMinutes = _series.Sum(s => s.TotalWatchedMinutes);
            int totalMinutes = movieMinutes + seriesMinutes;

            if (totalMinutes == 0) totalMinutes = 1; // Division by zero önleme

            int moviePercent = (int)Math.Round((double)movieMinutes / totalMinutes * 100);
            int seriesPercent = 100 - moviePercent;

            TotalWatchHoursText.Text = (totalMinutes / 60).ToString();
            MoviePercentText.Text = $"{moviePercent}%";
            SeriesPercentText.Text = $"{seriesPercent}%";
            MovieHoursText.Text = $"Filmler ({movieMinutes / 60}sa)";
            SeriesHoursText.Text = $"Diziler ({seriesMinutes / 60}sa)";

            // Progress bar column widths
            MovieProgressColumn.Width = new GridLength(moviePercent, GridUnitType.Star);
            SeriesProgressColumn.Width = new GridLength(seriesPercent, GridUnitType.Star);
        }

        private void LoadRecentItems()
        {
            var recentItems = new List<RecentMediaItem>();

            // Film ve dizileri birleştir
            foreach (var movie in _movies.OrderByDescending(m => m.AddedDate).Take(4))
            {
                recentItems.Add(new RecentMediaItem
                {
                    Id = movie.Id,
                    Title = movie.Title,
                    PosterUrl = movie.PosterUrl,
                    TypeDisplay = "Film",
                    IsMovie = true
                });
            }

            foreach (var series in _series.OrderByDescending(s => s.AddedDate).Take(4))
            {
                recentItems.Add(new RecentMediaItem
                {
                    Id = series.Id,
                    Title = series.Title,
                    PosterUrl = series.CoverImageUrl,
                    TypeDisplay = "Dizi",
                    IsMovie = false
                });
            }

            // Tarihe göre sırala ve ilk 4'ü al
            var sorted = recentItems.Take(4).ToList();

            if (sorted.Count > 0)
            {
                RecentItemsControl.ItemsSource = sorted;
                EmptyRecentState.Visibility = Visibility.Collapsed;
            }
            else
            {
                EmptyRecentState.Visibility = Visibility.Visible;
            }
        }

        private void LoadMonthlyWatchTime()
        {
            var monthlyData = new List<MonthlyWatchItem>();
            var today = DateTime.Now;

            for (int i = 5; i >= 0; i--)
            {
                var monthDate = today.AddMonths(-i);
                var monthStart = new DateTime(monthDate.Year, monthDate.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                // Bu ay izlenmiş film dakikaları
                int movieMinutes = _movies
                    .Where(m => m.WatchedDate.HasValue && m.WatchedDate >= monthStart && m.WatchedDate <= monthEnd)
                    .Sum(m => m.DurationMinutes);

                // Diziler için şu an tarih bazlı takip yok, basitçe AddedDate kullanıyoruz
                // Gerçek uygulamada bölüm izlenme tarihleri takip edilmeli
                int seriesMinutes = 0; // Placeholder

                int totalHours = (movieMinutes + seriesMinutes) / 60;

                monthlyData.Add(new MonthlyWatchItem
                {
                    MonthName = monthDate.ToString("MMM"),
                    Hours = totalHours
                });
            }

            WatchTimeSeries.DataSource = monthlyData;

            int totalMonthlyHours = monthlyData.Sum(m => m.Hours);
            double avgMonthlyHours = monthlyData.Average(m => m.Hours);

            MonthlyTotalHoursText.Text = $"{totalMonthlyHours} saat";
            MonthlyAvgHoursText.Text = $"{avgMonthlyHours:F0} /ay";
        }

        private void LoadFavoriteDirectors()
        {
            var directors = _movies
                .Where(m => !string.IsNullOrEmpty(m.Director))
                .GroupBy(m => m.Director)
                .Select(g => new FavoriteDirectorItem
                {
                    Name = g.Key,
                    MovieCount = g.Count(),
                    AvgRating = g.Where(m => m.UserRating.HasValue && m.UserRating > 0).Any()
                                ? g.Where(m => m.UserRating.HasValue && m.UserRating > 0).Average(m => m.UserRating ?? 0)
                                : 0,
                    Initials = GetInitials(g.Key)
                })
                .OrderByDescending(d => d.MovieCount)
                .ThenByDescending(d => d.AvgRating)
                .Take(4)
                .ToList();

            if (directors.Count > 0)
            {
                FavoriteDirectorsControl.ItemsSource = directors;
                EmptyDirectorsText.Visibility = Visibility.Collapsed;
            }
            else
            {
                EmptyDirectorsText.Visibility = Visibility.Visible;
            }
        }

        private void LoadDecadeAnalysis()
        {
            // Film + Dizi yıllarını birleştir
            var allYears = new List<int>();

            foreach (var movie in _movies.Where(m => m.Year > 0))
                allYears.Add(movie.Year);

            foreach (var series in _series.Where(s => !string.IsNullOrEmpty(s.Year) && s.Year != "N/A"))
            {
                if (int.TryParse(series.Year, out int year))
                    allYears.Add(year);
            }

            int count1990s = allYears.Count(y => y >= 1990 && y < 2000);
            int count2000s = allYears.Count(y => y >= 2000 && y < 2010);
            int count2010s = allYears.Count(y => y >= 2010 && y < 2020);
            int count2020s = allYears.Count(y => y >= 2020);

            int maxCount = Math.Max(Math.Max(count1990s, count2000s), Math.Max(count2010s, count2020s));
            double maxHeight = 80;

            Bar1990s.Height = maxCount > 0 ? (count1990s * maxHeight / maxCount) : 5;
            Bar2000s.Height = maxCount > 0 ? (count2000s * maxHeight / maxCount) : 5;
            Bar2010s.Height = maxCount > 0 ? (count2010s * maxHeight / maxCount) : 5;
            Bar2020s.Height = maxCount > 0 ? (count2020s * maxHeight / maxCount) : 5;

            // Minimum height
            if (Bar1990s.Height < 5) Bar1990s.Height = 5;
            if (Bar2000s.Height < 5) Bar2000s.Height = 5;
            if (Bar2010s.Height < 5) Bar2010s.Height = 5;
            if (Bar2020s.Height < 5) Bar2020s.Height = 5;
        }

        private void LoadFavoriteMovies()
        {
            _favoriteMovies = _movies.Where(m => m.IsFavorite).OrderByDescending(m => m.UserRating).ToList();

            if (_favoriteMovies.Count > 0)
            {
                _currentMovieIndex = 0;
                DisplayFavoriteMovie(_favoriteMovies[0]);
                FavoriteMovieCard.Visibility = Visibility.Visible;
                NoFavoriteMoviesText.Visibility = Visibility.Collapsed;
                PrevMovieBtn.Visibility = _favoriteMovies.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
                NextMovieBtn.Visibility = _favoriteMovies.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                FavoriteMovieCard.Visibility = Visibility.Collapsed;
                NoFavoriteMoviesText.Visibility = Visibility.Visible;
                PrevMovieBtn.Visibility = Visibility.Collapsed;
                NextMovieBtn.Visibility = Visibility.Collapsed;
            }
        }

        private void DisplayFavoriteMovie(Movie movie)
        {
            FavoriteMovieTitle.Text = movie.Title;
            FavoriteMovieYear.Text = movie.Year.ToString();
            FavoriteMovieRating.Text = (movie.UserRating ?? 0).ToString("F1");
            FavoriteMovieDirector.Text = movie.Director ?? "Yönetmen Yok";

            string imageUrl = !string.IsNullOrEmpty(movie.BackdropUrl) ? movie.BackdropUrl : movie.PosterUrl;
            if (!string.IsNullOrEmpty(imageUrl))
            {
                try { FavoriteMovieBgBrush.ImageSource = new BitmapImage(new Uri(imageUrl)); }
                catch { }
            }
        }

        private void LoadFavoriteSeries()
        {
            _favoriteSeries = _series.Where(s => s.IsFavorite).OrderByDescending(s => s.UserRating).ToList();

            if (_favoriteSeries.Count > 0)
            {
                _currentSeriesIndex = 0;
                DisplayFavoriteSeries(_favoriteSeries[0]);
                FavoriteSeriesCard.Visibility = Visibility.Visible;
                NoFavoriteSeriesText.Visibility = Visibility.Collapsed;
                PrevSeriesBtn.Visibility = _favoriteSeries.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
                NextSeriesBtn.Visibility = _favoriteSeries.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                FavoriteSeriesCard.Visibility = Visibility.Collapsed;
                NoFavoriteSeriesText.Visibility = Visibility.Visible;
                PrevSeriesBtn.Visibility = Visibility.Collapsed;
                NextSeriesBtn.Visibility = Visibility.Collapsed;
            }
        }

        private void DisplayFavoriteSeries(Series series)
        {
            FavoriteSeriesTitle.Text = series.Title;
            FavoriteSeriesInfo.Text = $"{series.TotalSeasons} Sezon • {series.TotalEpisodes} Bölüm";
            FavoriteSeriesRating.Text = series.UserRating.ToString("F1");
            FavoriteSeriesYears.Text = series.Year ?? "";

            string imageUrl = !string.IsNullOrEmpty(series.BackdropUrl) ? series.BackdropUrl : series.CoverImageUrl;
            if (!string.IsNullOrEmpty(imageUrl))
            {
                try { FavoriteSeriesBgBrush.ImageSource = new BitmapImage(new Uri(imageUrl)); }
                catch { }
            }
        }

        private void LoadLongestContent()
        {
            // En uzun film
            var longestMovie = _movies.OrderByDescending(m => m.DurationMinutes).FirstOrDefault();
            if (longestMovie != null)
            {
                LongestMovieTitle.Text = longestMovie.Title;
                LongestMovieDuration.Text = $"{longestMovie.DurationMinutes} dk";
                LongestMovieRating.Text = (longestMovie.UserRating ?? 0).ToString("F1");
                
                string imageUrl = !string.IsNullOrEmpty(longestMovie.BackdropUrl) ? longestMovie.BackdropUrl : longestMovie.PosterUrl;
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    try { LongestMovieBgBrush.ImageSource = new BitmapImage(new Uri(imageUrl)); }
                    catch { }
                }
            }

            // En uzun dizi (toplam bölüm sayısına göre)
            var longestSeries = _series.OrderByDescending(s => s.TotalEpisodes).FirstOrDefault();
            if (longestSeries != null)
            {
                LongestSeriesTitle.Text = longestSeries.Title;
                int totalMinutes = longestSeries.TotalEpisodes * (longestSeries.AvgEpisodeRuntime > 0 ? longestSeries.AvgEpisodeRuntime : 45);
                LongestSeriesInfo.Text = $"{longestSeries.TotalEpisodes} bölüm (~{totalMinutes / 60} saat)";
                LongestSeriesRating.Text = longestSeries.UserRating.ToString("F1");

                string imageUrl = !string.IsNullOrEmpty(longestSeries.BackdropUrl) ? longestSeries.BackdropUrl : longestSeries.CoverImageUrl;
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    try { LongestSeriesBgBrush.ImageSource = new BitmapImage(new Uri(imageUrl)); }
                    catch { }
                }
            }
        }

        private void ShowEmptyState()
        {
            TotalMoviesText.Text = "0";
            TotalSeriesText.Text = "0";
            TotalWatchTimeText.Text = "0";
            InProgressCountText.Text = "0";
            AvgRatingText.Text = "0.0";
            EmptyRecentState.Visibility = Visibility.Visible;
        }

        private string GetInitials(string name)
        {
            if (string.IsNullOrEmpty(name)) return "?";
            var parts = name.Split(' ');
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            return name.Length >= 2 ? name.Substring(0, 2).ToUpper() : name.ToUpper();
        }

        #region Navigation Events

        private void CurrentFavoriteMovie_Click(object sender, MouseButtonEventArgs e)
        {
            if (_favoriteMovies == null || _favoriteMovies.Count == 0) return;
            var movie = _favoriteMovies[_currentMovieIndex];
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null) mainWindow.NavigateToMovieDetail(movie);
        }

        private void CurrentFavoriteSeries_Click(object sender, MouseButtonEventArgs e)
        {
            if (_favoriteSeries == null || _favoriteSeries.Count == 0) return;
            var series = _favoriteSeries[_currentSeriesIndex];
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null) mainWindow.NavigateToSeriesDetail(series);
        }

        private void LongestMovie_Click(object sender, MouseButtonEventArgs e)
        {
            var longestMovie = _movies.OrderByDescending(m => m.DurationMinutes).FirstOrDefault();
            if (longestMovie != null)
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null) mainWindow.NavigateToMovieDetail(longestMovie);
            }
        }

        private void LongestSeries_Click(object sender, MouseButtonEventArgs e)
        {
             var longestSeries = _series.OrderByDescending(s => s.TotalEpisodes).FirstOrDefault();
             if (longestSeries != null)
             {
                 var mainWindow = Window.GetWindow(this) as MainWindow;
                 if (mainWindow != null) mainWindow.NavigateToSeriesDetail(longestSeries);
             }
        }

        private void PrevMovie_Click(object sender, RoutedEventArgs e)
        {
            if (_favoriteMovies == null || _favoriteMovies.Count <= 1) return;
            _currentMovieIndex = (_currentMovieIndex - 1 + _favoriteMovies.Count) % _favoriteMovies.Count;
            DisplayFavoriteMovie(_favoriteMovies[_currentMovieIndex]);
        }

        private void NextMovie_Click(object sender, RoutedEventArgs e)
        {
            if (_favoriteMovies == null || _favoriteMovies.Count <= 1) return;
            _currentMovieIndex = (_currentMovieIndex + 1) % _favoriteMovies.Count;
            DisplayFavoriteMovie(_favoriteMovies[_currentMovieIndex]);
        }

        private void PrevSeries_Click(object sender, RoutedEventArgs e)
        {
            if (_favoriteSeries == null || _favoriteSeries.Count <= 1) return;
            _currentSeriesIndex = (_currentSeriesIndex - 1 + _favoriteSeries.Count) % _favoriteSeries.Count;
            DisplayFavoriteSeries(_favoriteSeries[_currentSeriesIndex]);
        }

        private void NextSeries_Click(object sender, RoutedEventArgs e)
        {
            if (_favoriteSeries == null || _favoriteSeries.Count <= 1) return;
            _currentSeriesIndex = (_currentSeriesIndex + 1) % _favoriteSeries.Count;
            DisplayFavoriteSeries(_favoriteSeries[_currentSeriesIndex]);
        }

        private void ViewAllRecent_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to Movies or Series view
            var mainWindow = Window.GetWindow(this) as MainWindow;
            // Could show a selection dialog or default to Movies
        }

        private void RecentItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is RecentMediaItem item)
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow == null) return;

                if (item.IsMovie)
                {
                    var movie = _movies.FirstOrDefault(m => m.Id == item.Id);
                    if (movie != null)
                        mainWindow.NavigateToMovieDetail(movie);
                }
                else
                {
                    var series = _series.FirstOrDefault(s => s.Id == item.Id);
                    if (series != null)
                        mainWindow.NavigateToSeriesDetail(series);
                }
            }
        }

        #endregion
    }

    // Helper classes
    public class RecentMediaItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string PosterUrl { get; set; }
        public string TypeDisplay { get; set; }
        public bool IsMovie { get; set; }
    }

    public class MonthlyWatchItem
    {
        public string MonthName { get; set; }
        public int Hours { get; set; }
    }

    public class FavoriteDirectorItem
    {
        public string Name { get; set; }
        public int MovieCount { get; set; }
        public double AvgRating { get; set; }
        public string Initials { get; set; }
    }
}
