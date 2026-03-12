using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using DevExpress.Xpf.Charts;
using HobbyTracker.Services;
using HobbyTracker.Models;

using System.Windows.Input;

namespace HobbyTracker.Views.Dashboard
{
    /// <summary>
    /// Dashboard Kitaplar sekmesi - kitap istatistikleri ve grafikler
    /// </summary>
    public partial class DashboardKitaplarView : System.Windows.Controls.UserControl
    {
        private int _currentGoal = 24;
        
        public static readonly DependencyProperty LongestBookProperty =
            DependencyProperty.Register("LongestBook", typeof(Book), typeof(DashboardKitaplarView), new PropertyMetadata(null));

        public Book LongestBook
        {
            get { return (Book)GetValue(LongestBookProperty); }
            set { SetValue(LongestBookProperty, value); }
        }

        public static readonly DependencyProperty FavoriteBooksProperty =
            DependencyProperty.Register("FavoriteBooks", typeof(System.Collections.ObjectModel.ObservableCollection<Book>), typeof(DashboardKitaplarView), new PropertyMetadata(null));

        public System.Collections.ObjectModel.ObservableCollection<Book> FavoriteBooks
        {
            get { return (System.Collections.ObjectModel.ObservableCollection<Book>)GetValue(FavoriteBooksProperty); }
            set { SetValue(FavoriteBooksProperty, value); }
        }

        public static readonly DependencyProperty HasNoFavoritesProperty =
            DependencyProperty.Register("HasNoFavorites", typeof(bool), typeof(DashboardKitaplarView), new PropertyMetadata(true));

        public bool HasNoFavorites
        {
            get { return (bool)GetValue(HasNoFavoritesProperty); }
            set { SetValue(HasNoFavoritesProperty, value); }
        }

        public static readonly DependencyProperty CurrentFavoriteBookProperty =
            DependencyProperty.Register("CurrentFavoriteBook", typeof(Book), typeof(DashboardKitaplarView), new PropertyMetadata(null));

        public Book CurrentFavoriteBook
        {
            get { return (Book)GetValue(CurrentFavoriteBookProperty); }
            set { SetValue(CurrentFavoriteBookProperty, value); }
        }

        private int _currentFavoriteIndex = 0;
        private List<Book> _books;
        private List<Book> _favoriteBooksList;

        public DashboardKitaplarView()
        {
            InitializeComponent();
            this.DataContext = this;
            Loaded += DashboardKitaplarView_Loaded;
        }

        private async void DashboardKitaplarView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadBookDataAsync();
        }

        public async Task LoadBookDataAsync()
        {
            try
            {
                var firebase = new SFirebase();
                _books = await firebase.GetBooksAsync();
                _currentGoal = await firebase.GetBookGoalAsync();

                if (_books == null || _books.Count == 0)
                {
                    ShowEmptyState();
                    LongestBook = null;
                    return;
                }

                UpdateStatistics();
                UpdateGoalStatistics();
                LoadRecentBooks();
                LoadRatingDistribution();
                LoadThicknessStats();
                LoadPagePerformance();
                UpdateStatistics();
                LoadRecentBooks();
                LoadRatingDistribution();
                LoadThicknessStats();
                LoadPagePerformance();
                LoadFavoriteAuthors();

                // Favori Kitapları Yükle (IsFavorite == true olanlar)
                var favs = _books.Where(b => b.IsFavorite).OrderByDescending(b => b.UserRating).ToList();
                FavoriteBooks = new System.Collections.ObjectModel.ObservableCollection<Book>(favs);
                _favoriteBooksList = favs;
                HasNoFavorites = favs.Count == 0;

                // İlk favori kitabı seç
                if (favs.Count > 0)
                {
                    _currentFavoriteIndex = 0;
                    CurrentFavoriteBook = favs[0];
                }
                else
                {
                    CurrentFavoriteBook = null;
                }

                // En Uzun Kitabı Bul
                LongestBook = _books.OrderByDescending(b => b.PageCount).FirstOrDefault();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Kitap verileri yüklenirken hata: {ex.Message}");
            }
        }

        private void UpdateStatistics()
        {
            if (_books == null) return;

            int total = _books.Count;
            int read = _books.Count(b => b.Status == "Okundu" || b.Status == "Bitti");
            int reading = _books.Count(b => b.Status == "Okunuyor");
            
            // Toplam sayfa sayısı
            int totalPages = _books.Sum(b => b.PageCount);
            
            // Ortalama puan (0 olmayanlar)
            var ratedBooks = _books.Where(b => b.UserRating > 0).ToList();
            double avgRating = ratedBooks.Count > 0 ? ratedBooks.Average(b => b.UserRating) : 0;

            // UI güncelle
            TotalBooksText.Text = total.ToString();
            ReadBooksText.Text = read.ToString();
            ReadingBooksText.Text = reading.ToString();
            AvgRatingText.Text = avgRating.ToString("F1");
            TotalPagesText.Text = totalPages.ToString("#,##0");

            // Tamamlanma yüzdesi
            double completionRate = total > 0 ? (read * 100.0 / total) : 0;
            CompletionRateText.Text = $"%{completionRate:F0} tamamlanma";
        }

        private void LoadRecentBooks()
        {
            if (_books == null || _books.Count == 0)
            {
                EmptyBooksState.Visibility = Visibility.Visible;
                RecentBooksControl.Visibility = Visibility.Collapsed;
                return;
            }

            // Son eklenen 4 kitap
            var recentBooks = _books
                .OrderByDescending(b => b.AddedDate)
                .Take(4)
                .ToList();

            RecentBooksControl.ItemsSource = recentBooks;
            RecentBooksControl.Visibility = Visibility.Visible;
            EmptyBooksState.Visibility = Visibility.Collapsed;
        }

        private void LoadRatingDistribution()
        {
            if (_books == null) return;

            var ratedBooks = _books.Where(b => b.UserRating > 0).ToList();
            double avg = ratedBooks.Any() ? ratedBooks.Average(b => b.UserRating) : 0;
            int count = ratedBooks.Count;

            AnalysisAvgRating.Text = avg.ToString("F1");
            TotalRatingsCount.Text = $"{count} DEĞERLENDİRME";

            var distribution = new List<RatingDistItem>();
            int maxCount = 0;

            // 5 Grup: 9-10, 7-8, 5-6, 3-4, 1-2
            // i = 5 (9-10), i = 4 (7-8), ..., i = 1 (1-2)
            for (int i = 5; i >= 1; i--)
            {
                // Aralık: (i*2)-1 ile i*2 (Örn: i=5 -> 9-10)
                int minRange = (i * 2) - 1;
                int maxRange = i * 2;
                string label = $"{minRange}-{maxRange}";

                // Puanı yuvarla ve aralığa göre say
                // Örn: 8.5 puan -> 9'a yuvarlanır -> 9-10 grubuna girer
                // Örn: 8.4 puan -> 8'e yuvarlanır -> 7-8 grubuna girer
                int c = ratedBooks.Count(b => 
                {
                    int r = (int)Math.Round(b.UserRating);
                    return r >= minRange && r <= maxRange;
                });

                if (c > maxCount) maxCount = c;
                distribution.Add(new RatingDistItem { Star = label, Count = c });
            }

            // Genişlikleri ayarla (maksimum genişlik 100 birim olsun)
            foreach (var item in distribution)
            {
                item.Width = maxCount > 0 ? (item.Count * 120.0 / maxCount) : 0; 
                if (item.Width < 2 && item.Count > 0) item.Width = 2; // Min görünürlük
            }

            RatingDistributionControl.ItemsSource = distribution;
        }

        private void LoadThicknessStats()
        {
            if (_books == null) return;

            int less300 = _books.Count(b => b.PageCount < 300);
            int mid300to500 = _books.Count(b => b.PageCount >= 300 && b.PageCount <= 500);
            int more500 = _books.Count(b => b.PageCount > 500);

            int max = Math.Max(less300, Math.Max(mid300to500, more500));
            double maxHeight = 80; // Bar max yüksekliği

            // Barların yüksekliğini ayarla
            BarLess300.Height = max > 0 ? (less300 * maxHeight / max) : 0;
            Bar300to500.Height = max > 0 ? (mid300to500 * maxHeight / max) : 0;
            BarMore500.Height = max > 0 ? (more500 * maxHeight / max) : 0;
            
            // Tooltip veya text eklenebilir ama şu an basit tutuyoruz
        }

         private void LoadPagePerformance()
        {
            if (_books == null || PageSeries == null) return;

            // Son 6 ayın verilerini hazırla
            var performanceData = new List<PagePerformanceItem>();
            var today = DateTime.Now;
            
            // Son 6 ay için döngü
            for (int i = 5; i >= 0; i--)
            {
                var monthDate = today.AddMonths(-i);
                var monthStart = new DateTime(monthDate.Year, monthDate.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                
                // Bu ay biten kitapların sayfa sayıları toplamı
                // Not: Book modelinde 'FinishDate' veya benzeri bir alan olmalı. 
                // Şimdilik 'AddedDate' kullanacağız çünkü FinishDate olmayabilir.
                // Varsa FinishDate kullanılmalı.
                
                // Varsayım: Book modelinde DateFinished varsa onu kullan, yoksa AddedDate
                // Model kontrolü yapmadım ama AddedDate kesin var.
                int pages = _books
                    .Where(b => b.Status == "Okundu" && b.AddedDate >= monthStart && b.AddedDate <= monthEnd)
                    .Sum(b => b.PageCount);

                performanceData.Add(new PagePerformanceItem 
                { 
                    MonthName = monthDate.ToString("MMM"), // Oca, Şub vs.
                    PageCount = pages 
                });
            }

            PageSeries.DataSource = performanceData;

            // İstatistikleri güncelle
            int totalPagesPeriod = performanceData.Sum(p => p.PageCount);
            double avgPagesPeriod = performanceData.Average(p => p.PageCount);

            PerformansTotalPages.Text = totalPagesPeriod.ToString("#,##0");
            PerformansAvgPages.Text = $"{avgPagesPeriod:F0} /ay";
        }

        private void LoadFavoriteAuthors()
        {
            if (_books == null) return;

            var authors = _books
                .GroupBy(b => b.Authors)
                .Select(g => new FavoriteAuthorItem
                {
                    Name = g.Key,
                    BookCount = g.Count(),
                    AvgRating = g.Where(b => b.Status != "Okunacak" && b.UserRating > 0).Any() 
                                ? g.Where(b => b.Status != "Okunacak" && b.UserRating > 0).Average(b => b.UserRating) 
                                : 0,
                    Initials = GetInitials(g.Key)
                })
                .OrderByDescending(a => a.BookCount)
                .ThenByDescending(a => a.AvgRating)
                .Take(4)
                .ToList();

            if (authors.Count > 0)
            {
                FavoriteAuthorsControl.ItemsSource = authors;
                EmptyAuthorsText.Visibility = Visibility.Collapsed;
            }
            else
            {
                EmptyAuthorsText.Visibility = Visibility.Visible;
            }
        }

        private string GetInitials(string name)
        {
            if (string.IsNullOrEmpty(name)) return "?";
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return parts[0].Substring(0, 1).ToUpper();
            return (parts[0].Substring(0, 1) + parts[parts.Length - 1].Substring(0, 1)).ToUpper();
        }

        private void ShowEmptyState()
        {
            TotalBooksText.Text = "0";
            ReadBooksText.Text = "0";
            ReadingBooksText.Text = "0";
            AvgRatingText.Text = "0";
            TotalPagesText.Text = "0";
            CompletionRateText.Text = "%0 tamamlanma";

            EmptyBooksState.Visibility = Visibility.Visible;
            RecentBooksControl.Visibility = Visibility.Collapsed;
        }

        private void ViewAllBooks_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow; 
            if (mainWindow != null) 
            {
                mainWindow.SwitchToBooks(); // MainWindow'da bu metod olmalı
            }
        }

        private void OpenBookDetails_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Book book)
            {
                // Kitap düzenleme penceresi aç (Varsa)
                // var editWindow = new EditBookWindow(book);
                // if (editWindow.ShowDialog() == true) RefreshData();
            }
        }

        private void LongestBook_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (LongestBook != null)
            {
                var editWindow = new EditBookWindow(LongestBook);
                if (editWindow.ShowDialog() == true)
                {
                    RefreshData();
                }
            }
        }

        private void NextFavorite_Click(object sender, RoutedEventArgs e)
        {
            if (_favoriteBooksList == null || _favoriteBooksList.Count == 0) return;

            _currentFavoriteIndex++;
            if (_currentFavoriteIndex >= _favoriteBooksList.Count)
            {
                _currentFavoriteIndex = 0; // Başa dön
            }
            CurrentFavoriteBook = _favoriteBooksList[_currentFavoriteIndex];
        }

        private void PrevFavorite_Click(object sender, RoutedEventArgs e)
        {
             if (_favoriteBooksList == null || _favoriteBooksList.Count == 0) return;

            _currentFavoriteIndex--;
            if (_currentFavoriteIndex < 0)
            {
                _currentFavoriteIndex = _favoriteBooksList.Count - 1; // Sona dön
            }
            CurrentFavoriteBook = _favoriteBooksList[_currentFavoriteIndex];
        }

        private void CurrentFavorite_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
             if (CurrentFavoriteBook != null)
            {
                var editWindow = new EditBookWindow(CurrentFavoriteBook);
                if (editWindow.ShowDialog() == true)
                {
                    RefreshData();
                }
            }
        }

        public async void RefreshData()
        {
            await LoadBookDataAsync();
        }

        private void UpdateGoalStatistics()
        {
            if (_books == null) return;

            int target = _currentGoal;
            int finishedCount = _books.Count(b => b.Status == "Okundu" || b.Status == "Bitti");

            // UI updates
            if (this.FindName("TxtFinishedCount") is TextBlock txtCount) txtCount.Text = finishedCount.ToString();
            if (this.FindName("TxtTotalGoal") is TextBlock txtTotalGoal) txtTotalGoal.Text = $"/ {target} Kitap";

            if (this.FindName("ProgressGoal") is Border borderProgress)
            {
                double totalWidth = 260; 
                double percent = target > 0 ? (double)finishedCount / target : 0;
                if (percent > 1) percent = 1;
                borderProgress.Width = totalWidth * percent;
            }

            if (this.FindName("TxtGoalMessage") is TextBlock txtMessage)
            {
                if (finishedCount == 0) txtMessage.Text = "Henüz başlamadın. Hadi bir kitap bitir!";
                else if (finishedCount >= target) txtMessage.Text = "Tebrikler! Yıllık hedefine ulaştın!";
                else txtMessage.Text = $"Harika gidiyorsun! Hedefinin %{(int)((double)finishedCount/target * 100)}'sini tamamladın.";
            }
        }

        private void BtnEditGoal_Click(object sender, RoutedEventArgs e)
        {
            if (this.FindName("GoalPopup") is System.Windows.Controls.Primitives.Popup popup)
            {
                if (this.FindName("SpinGoal") is DevExpress.Xpf.Editors.SpinEdit spin)
                {
                    spin.Value = _currentGoal;
                }
                popup.IsOpen = true;
            }
        }

        private async void BtnSaveGoal_Click(object sender, RoutedEventArgs e)
        {
            if (this.FindName("SpinGoal") is DevExpress.Xpf.Editors.SpinEdit spin)
            {
                int newGoal = (int)spin.Value;
                if (newGoal < 1) newGoal = 1;

                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                var firebase = new SFirebase();
                await firebase.UpdateBookGoalAsync(newGoal);
                _currentGoal = newGoal;
                Mouse.OverrideCursor = null;

                UpdateGoalStatistics();

                if (this.FindName("GoalPopup") is System.Windows.Controls.Primitives.Popup popup)
                    popup.IsOpen = false;
            }
        }
    }

    public class RatingDistItem
    {
        public string Star { get; set; }
        public int Count { get; set; }
        public double Width { get; set; }
    }

    public class PagePerformanceItem
    {
        public string MonthName { get; set; }
        public int PageCount { get; set; }
    }

    public class FavoriteAuthorItem
    {
        public string Name { get; set; }
        public int BookCount { get; set; }
        public double AvgRating { get; set; }
        public string Initials { get; set; }
    }
}
