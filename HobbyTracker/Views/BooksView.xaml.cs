using DevExpress.Xpf.Core;
using HobbyTracker.Models;
using HobbyTracker.Services;
using System.Collections.Generic;
using System.Linq; // Added for LINQ extension methods (Where, ToList)
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HobbyTracker.Views
{
    public partial class BooksView : System.Windows.Controls.UserControl
    {
        private SFirebase _firebaseService;
        private List<Book> _allBooks = new List<Book>();
        private string _currentFilter = "Tümü";

        private int _currentGoal = 24; // Varsayılan hedef
        
        public BooksView()
        {
            InitializeComponent();
            _firebaseService = new SFirebase();
            this.Loaded += BooksView_Loaded;
        }

        private async void BooksView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadBooksAsync();
        }

        private async System.Threading.Tasks.Task LoadBooksAsync()
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            
            // Firebase'den kitapları çek
            _allBooks = await _firebaseService.GetBooksAsync();
            
            // Hedefi çek
            _currentGoal = await _firebaseService.GetBookGoalAsync();

            // Filtreleri uygula
            ApplyFilters();
            
            // İstatistikleri Hesapla
            CalculateStats();

            Mouse.OverrideCursor = null;
        }

        private void CalculateStats()
        {
            // --- 1. Yıllık Hedef ---
            int target = _currentGoal;
            int finishedCount = _allBooks.Count(b => b.Status == "Bitti" || b.Status == "Okundu");
            
            // Kontroller null değilse güncelle (FindName ile de alınabilir ama x:Name verdik)
            var txtCount = this.FindName("TxtFinishedCount") as TextBlock;
            var txtTotalGoal = this.FindName("TxtTotalGoal") as TextBlock;
            var borderProgress = this.FindName("ProgressGoal") as Border;
            var txtMessage = this.FindName("TxtGoalMessage") as TextBlock;

            if (txtCount != null) txtCount.Text = finishedCount.ToString();
            if (txtTotalGoal != null) txtTotalGoal.Text = $"/ {target} Kitap";
            
            if (borderProgress != null)
            {
                // Toplam genişlik yaklaşık 260px (Panel 300 - Padding 40)
                double totalWidth = 260;
                double percent = (double)finishedCount / target;
                if (percent > 1) percent = 1;
                borderProgress.Width = totalWidth * percent;
            }

            if (txtMessage != null)
            {
                if (finishedCount == 0) txtMessage.Text = "Henüz başlamadın. Hadi bir kitap bitir!";
                else if (finishedCount >= target) txtMessage.Text = "Tebrikler! Yıllık hedefine ulaştın!";
                else txtMessage.Text = $"Harika gidiyorsun! Hedefinin %{(int)((double)finishedCount/target*100)}'sini tamamladın.";
            }

            // --- 2. Detaylı İstatistikler Paneli ---
            UpdateBookStats();

            // --- 3. Favori Yazarlar ---
            // Yazarları ayır ve say
            var authorStats = _allBooks
                .Where(b => !string.IsNullOrEmpty(b.Authors))
                .SelectMany(b => b.Authors.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries))
                .Select(a => a.Trim())
                .GroupBy(a => a)
                .Select(g => new AuthorStat 
                { 
                    Name = g.Key, 
                    Count = g.Count() 
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            var authorsList = this.FindName("AuthorsList") as System.Windows.Controls.ItemsControl;
            if (authorsList != null)
            {
                authorsList.ItemsSource = authorStats;
            }
        }
        
        private readonly StatsTrendService _trendService = new StatsTrendService();

        /// <summary>
        /// Detaylı istatistikler panelini günceller
        /// </summary>
        private void UpdateBookStats()
        {
            // Toplam kitap sayısı
            int totalBooks = _allBooks.Count;
            
            // Okunan kitap sayısı (sadece "Okundu" durumundakiler)
            int readBooks = _allBooks.Count(b => b.Status == "Okundu" || b.Status == "Bitti");

            // Okunuyor kitap sayısı
            int readingBooks = _allBooks.Count(b => b.Status == "Okunuyor");
            
            // Toplam okunan sayfa sayısı (Okundu, Okunuyor, Yarım Bırakıldı)
            // Okundu olanlar için toplam sayfa, diğerleri için CurrentPage
            int totalPages = _allBooks
                .Where(b => b.Status == "Okundu" || b.Status == "Bitti" || b.Status == "Okunuyor" || b.Status == "Yarım Bırakıldı")
                .Sum(b => {
                    if (b.Status == "Okundu" || b.Status == "Bitti")
                        return b.PageCount > 0 ? b.PageCount : 0;
                    else
                        return b.CurrentPage > 0 ? b.CurrentPage : 0;
                });
            
            // Ortalama rating (Okundu, Okunuyor, Yarım Bırakıldı - rating > 0 olanlar)
            var ratedBooks = _allBooks
                .Where(b => (b.Status == "Okundu" || b.Status == "Bitti" || b.Status == "Okunuyor" || b.Status == "Yarım Bırakıldı") 
                            && b.UserRating > 0)
                .ToList();
            double avgRating = ratedBooks.Any() ? ratedBooks.Average(b => b.UserRating) : 0;
            
            // UI Güncelle
            if (TxtTotalBooks != null) TxtTotalBooks.Text = totalBooks.ToString();
            if (TxtReadBooks != null) TxtReadBooks.Text = readBooks.ToString();
            if (TxtReadingBooks != null) TxtReadingBooks.Text = readingBooks.ToString();
            if (TxtTotalPages != null) TxtTotalPages.Text = FormatPageCount(totalPages);
            if (TxtAvgRating != null) TxtAvgRating.Text = avgRating.ToString("F1");
            
            // Özet satırı güncelle
            if (StatsText != null) StatsText.Text = $"Toplam {totalBooks} kitap • {readBooks} okundu";
            
            // Trend ikonlarını güncelle
            UpdateTrendIcon(TrendTotalBooks, StatsTrendService.Keys.BooksTotal, totalBooks);
            UpdateTrendIcon(TrendReadBooks, StatsTrendService.Keys.BooksRead, readBooks);
            UpdateTrendIcon(TrendReadingBooks, StatsTrendService.Keys.BooksReading, readingBooks);
            UpdateTrendIcon(TrendPages, StatsTrendService.Keys.BooksPages, totalPages);
            UpdateTrendIcon(TrendRating, StatsTrendService.Keys.BooksRating, avgRating);
            
            // Yeni değerleri kaydet
            _trendService.SaveValue(StatsTrendService.Keys.BooksTotal, totalBooks);
            _trendService.SaveValue(StatsTrendService.Keys.BooksRead, readBooks);
            _trendService.SaveValue(StatsTrendService.Keys.BooksReading, readingBooks);
            _trendService.SaveValue(StatsTrendService.Keys.BooksPages, totalPages);
            _trendService.SaveValue(StatsTrendService.Keys.BooksRating, avgRating);
            _trendService.SaveToFile();
        }
        
        /// <summary>
        /// Trend ikonunu ve rengini günceller
        /// </summary>
        private void UpdateTrendIcon(System.Windows.Controls.TextBlock trendIcon, string key, double currentValue)
        {
            if (trendIcon == null) return;
            
            trendIcon.Text = _trendService.GetTrendIcon(key, currentValue);
            trendIcon.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(
                    _trendService.GetTrendColor(key, currentValue, "#6b7280")
                )
            );
        }

        /// <summary>
        /// Sayfa sayısını formatlar (1000+ için K kullanır)
        /// </summary>
        private string FormatPageCount(int pages)
        {
            if (pages >= 1000)
                return $"{pages / 1000.0:F1}K";
            return pages.ToString();
        }

        /// <summary>
        /// StatsText tıklandığında istatistik panelini açar/kapatır
        /// </summary>
        private void StatsText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DetailedStatsPanel != null)
            {
                DetailedStatsPanel.Visibility = DetailedStatsPanel.Visibility == Visibility.Visible 
                    ? Visibility.Collapsed 
                    : Visibility.Visible;
            }
        }

        /// <summary>
        /// DetailedStatsPanel tıklandığında Dashboard Kitaplar sekmesine yönlendirir
        /// </summary>
        private void DetailedStatsPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.NavigateToDashboardAndSelectTab("TabKitaplar");
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
                await _firebaseService.UpdateBookGoalAsync(newGoal);
                _currentGoal = newGoal;
                Mouse.OverrideCursor = null;
                
                CalculateStats();

                if (this.FindName("GoalPopup") is System.Windows.Controls.Primitives.Popup popup)
                    popup.IsOpen = false;
            }
        }

        public class AuthorStat
    {
        public string Name { get; set; }
        public int Count { get; set; }
        
        public string Initials
        {
            get
            {
                if (string.IsNullOrEmpty(Name)) return "?";
                var parts = Name.Split(' ');
                if (parts.Length == 1) return parts[0].Substring(0, 1).ToUpper();
                return (parts[0].Substring(0, 1) + parts[parts.Length - 1].Substring(0, 1)).ToUpper();
            }
        }

        public string BookCountText => $"{Count} Kitap";
    }


        private HashSet<string> _activeFilters = new HashSet<string>();
        private string _currentSortOption = "Date_Desc";

        private void FilterRadioButton_Checked(object sender, RoutedEventArgs e)
        {
             if (sender is System.Windows.Controls.RadioButton rb)
            {
                // Tag varsa onu kullan (UI'da 'Okundu' yazıp arkada 'Bitti' filtrelemek için)
                if (rb.Tag != null)
                    _currentFilter = rb.Tag.ToString();
                else if (rb.Content != null)
                    _currentFilter = rb.Content.ToString();
                
                ApplyFilters();
            }
        }

        private void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            if (this.FindName("FilterPopup") is System.Windows.Controls.Primitives.Popup popup)
                popup.IsOpen = true;
        }

        private void BtnSort_Click(object sender, RoutedEventArgs e)
        {
            if (this.FindName("SortPopup") is System.Windows.Controls.Primitives.Popup popup)
                popup.IsOpen = true;
        }

        private void BtnClearFilters_Click(object sender, RoutedEventArgs e)
        {
            _activeFilters.Clear();
            
            // Tüm Checkboxları temizle (Visual Tree'de gezmek yerine state ile yönetiyoruz ama UI reset lazım)
            // Kısa yol: Popup yeniden yüklenince resetlenmez, manuel kapatmak lazım.
            // En temizi: _activeFilters temizleyip ApplyFilters çağırınca filtre kalkar, ama Checkboxlar işaretli kalır.
            // UI tutarlılığı için Checkboxların IsChecked'ini false yapmalıyız.
            if (this.FindName("FilterPopup") is System.Windows.Controls.Primitives.Popup popup && popup.Child is Border border && border.Child is StackPanel panel)
            {
                ResetCheckBoxes(panel);
            }
            
            if (this.FindName("FilterPopup") is System.Windows.Controls.Primitives.Popup p)
                p.IsOpen = false;

            ApplyFilters();
        }

        private void ResetCheckBoxes(DependencyObject parent)
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is DevExpress.Xpf.Editors.CheckEdit checkEdit)
                {
                    checkEdit.IsChecked = false;
                }
                else
                {
                    ResetCheckBoxes(child);
                }
            }
        }

        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is DevExpress.Xpf.Editors.CheckEdit checkEdit && checkEdit.Tag != null)
            {
                string tag = checkEdit.Tag.ToString();
                if (checkEdit.IsChecked == true)
                    _activeFilters.Add(tag);
                else
                    _activeFilters.Remove(tag);
                
                ApplyFilters();
            }
        }

        private void SortOption_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.ListBox listBox && listBox.SelectedItem != null)
            {
                if (listBox.SelectedItem is System.Windows.Controls.ListBoxItem item && item.Tag != null)
                {
                    _currentSortOption = item.Tag.ToString();
                    ApplyFilters();
                    
                    // Popup'ı kapat
                    if (this.FindName("SortPopup") is System.Windows.Controls.Primitives.Popup popup)
                        popup.IsOpen = false;
                }
            }
        }

        private void ApplyFilters()
        {
            var query = _allBooks as IEnumerable<Book>;

            // 1. Arama Metni
            var txtSearch = this.FindName("TxtSearch") as DevExpress.Xpf.Editors.TextEdit;
            if (txtSearch != null && !string.IsNullOrEmpty(txtSearch.Text))
            {
                string searchText = txtSearch.Text.ToLower();
                query = query.Where(b => (b.Title != null && b.Title.ToLower().Contains(searchText)) || 
                                         (b.Authors != null && b.Authors.ToLower().Contains(searchText)));
            }

            // 2. Status Filtresi (Tablar)
            if (_currentFilter != "Tümü")
            {
                if (_currentFilter == "Okundu" || _currentFilter == "Bitti")
                {
                    query = query.Where(b => b.Status == "Okundu" || b.Status == "Bitti");
                }
                else
                {
                    query = query.Where(b => b.Status == _currentFilter);
                }
            }

            // 3. Detaylı Filtreler (Rating)
            // Eğer rating filtrelerinden HİÇBİRİ seçili değilse HEPSİ gösterilir.
            // Eğer BİR veya DAHA FAZLA seçiliyse, SADECE onlara uyanlar (OR mantığı) gösterilir.
            var ratingFilters = _activeFilters.Where(f => f.StartsWith("Rating_")).ToList();
            if (ratingFilters.Any())
            {
                query = query.Where(b => 
                    (ratingFilters.Contains("Rating_9_10") && b.UserRating >= 9 && b.UserRating <= 10) ||
                    (ratingFilters.Contains("Rating_7_8") && b.UserRating >= 7 && b.UserRating < 9) ||
                    (ratingFilters.Contains("Rating_5_6") && b.UserRating >= 5 && b.UserRating < 7) ||
                    (ratingFilters.Contains("Rating_Unrated") && b.Score == 0)
                );
            }

            // 4. Detaylı Filtreler (Page Count)
            var pageFilters = _activeFilters.Where(f => f.StartsWith("Pages_")).ToList();
            if (pageFilters.Any())
            {
                query = query.Where(b => 
                    (pageFilters.Contains("Pages_Short") && b.PageCount < 150) ||
                    (pageFilters.Contains("Pages_Medium") && b.PageCount >= 150 && b.PageCount <= 400) ||
                    (pageFilters.Contains("Pages_Long") && b.PageCount > 400)
                );
            }

            // 5. Sıralama
            switch (_currentSortOption)
            {
                case "Date_Desc": query = query.OrderByDescending(b => b.AddedDate); break;
                case "Date_Asc": query = query.OrderBy(b => b.AddedDate); break;
                case "Title_Asc": query = query.OrderBy(b => b.Title ?? ""); break;
                case "Title_Desc": query = query.OrderByDescending(b => b.Title ?? ""); break;
                case "Rating_Desc": query = query.OrderByDescending(b => b.UserRating); break;
                case "Rating_Asc": query = query.OrderBy(b => b.UserRating); break;
                case "Pages_Desc": query = query.OrderByDescending(b => b.PageCount); break;
                case "Pages_Asc": query = query.OrderBy(b => b.PageCount); break;
                case "Progress_Desc": query = query.OrderByDescending(b => b.ProgressPercentage); break;
                case "Progress_Asc": query = query.OrderBy(b => b.ProgressPercentage); break;
                case "Year_Desc": query = query.OrderByDescending(b => b.PublishedDate ?? ""); break;
                default: query = query.OrderByDescending(b => b.AddedDate); break;
            }

            // 6. Yazar Gruplama
            // Eğer yazar gruplama seçiliyse, önce yazara göre sırala (birincil), sonra mevcut sıralamayı ikincil olarak uygula
            if (_activeFilters.Contains("Group_Author"))
            {
                // Yazara göre birincil sıralama yap, sonra mevcut sıralamayı ikincil olarak uygula
                var authorSorted = query.OrderBy(b => b.Authors ?? "");
                
                switch (_currentSortOption)
                {
                    case "Date_Desc": query = authorSorted.ThenByDescending(b => b.AddedDate); break;
                    case "Date_Asc": query = authorSorted.ThenBy(b => b.AddedDate); break;
                    case "Title_Asc": query = authorSorted.ThenBy(b => b.Title ?? ""); break;
                    case "Title_Desc": query = authorSorted.ThenByDescending(b => b.Title ?? ""); break;
                    case "Rating_Desc": query = authorSorted.ThenByDescending(b => b.UserRating); break;
                    case "Rating_Asc": query = authorSorted.ThenBy(b => b.UserRating); break;
                    case "Pages_Desc": query = authorSorted.ThenByDescending(b => b.PageCount); break;
                    case "Pages_Asc": query = authorSorted.ThenBy(b => b.PageCount); break;
                    case "Progress_Desc": query = authorSorted.ThenByDescending(b => b.ProgressPercentage); break;
                    case "Progress_Asc": query = authorSorted.ThenBy(b => b.ProgressPercentage); break;
                    case "Year_Desc": query = authorSorted.ThenByDescending(b => b.PublishedDate ?? ""); break;
                    default: query = authorSorted.ThenByDescending(b => b.AddedDate); break;
                }
            }

            var booksList = query.ToList();
            
            // Grid görünümü (Add butonu dahil)
            var booksGridControl = this.FindName("BooksGridControl") as System.Windows.Controls.ItemsControl;
            if (booksGridControl != null)
            {
                var booksWithAddCard = new List<object>(booksList);
                booksWithAddCard.Add("Add"); // "Yeni Kitap Ekle" kartı için
                booksGridControl.ItemsSource = booksWithAddCard;
            }
            
            // Liste görünümü
            var booksListControl = this.FindName("BooksListControl") as System.Windows.Controls.ItemsControl;
            if (booksListControl != null)
            {
                booksListControl.ItemsSource = booksList;
            }
        }

        private void TxtSearch_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            ApplyFilters();
        }

        private async void BtnAddBook_Click(object sender, RoutedEventArgs e)
        {
            // Ekleme penceresini aç
            AddBookWindow addWindow = new AddBookWindow();
            addWindow.Owner = Window.GetWindow(this); // Ana pencerenin ortasında açılsın
            addWindow.ShowDialog(); // ShowDialog waits for close, but doesn't strictly await the result task same way. 
            // However, we want to refresh AFTER it closes.
            
            // Pencere kapandığında listeyi yenile
            await LoadBooksAsync();
        }

        private string _viewMode = "Grid";

        private void BtnViewGrid_Click(object sender, RoutedEventArgs e)
        {
            if (_viewMode != "Grid")
            {
                _viewMode = "Grid";
                UpdateViewMode();
            }
        }

        private void BtnViewList_Click(object sender, RoutedEventArgs e)
        {
            if (_viewMode != "List")
            {
                _viewMode = "List";
                UpdateViewMode();
            }
        }

        private void UpdateViewMode()
        {
            var booksGridControl = this.FindName("BooksGridControl") as System.Windows.Controls.ItemsControl;
            var booksListControl = this.FindName("BooksListControl") as System.Windows.Controls.ItemsControl;
            var btnViewGrid = this.FindName("BtnViewGrid") as System.Windows.Controls.Control;
            var btnViewList = this.FindName("BtnViewList") as System.Windows.Controls.Control;
            
            // Buton Görünümlerini Güncelle
            if (btnViewGrid != null && btnViewList != null)
            {
                if (_viewMode == "List")
                {
                    btnViewGrid.Opacity = 0.5;
                    btnViewList.Opacity = 1.0;
                }
                else
                {
                    btnViewGrid.Opacity = 1.0;
                    btnViewList.Opacity = 0.5;
                }
            }
            
            // Görünümleri değiştir
            if (_viewMode == "List")
            {
                if (booksGridControl != null) booksGridControl.Visibility = Visibility.Collapsed;
                if (booksListControl != null) booksListControl.Visibility = Visibility.Visible;
            }
            else
            {
                if (booksGridControl != null) booksGridControl.Visibility = Visibility.Visible;
                if (booksListControl != null) booksListControl.Visibility = Visibility.Collapsed;
            }
        }
        
        private async void BookCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is Book book)
            {
                // Düzenleme penceresini aç
                EditBookWindow editWindow = new EditBookWindow(book);
                
                // Opacity mask vs blur efekti için ana pencere referansı gerekebilir ama şimdilik direkt açıyoruz
                bool? result = editWindow.ShowDialog();

                // Eğer kaydedildiyse veya silindiyse listeyi yenile
                if (result == true)
                {
                    await LoadBooksAsync();
                }
            }
        }
    }
}