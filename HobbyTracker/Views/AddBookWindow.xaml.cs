using HobbyTracker.Models;
using HobbyTracker.Services;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace HobbyTracker.Views
{
    public partial class AddBookWindow : Window
    {
        private GoogleBooksService _bookService;
        private Book? _selectedBook;
        private List<Book> _searchResults = new List<Book>();
        
        // Yıldız puanlama için
        private double _currentRating = 0;
        private const int StarCount = 10;

        public AddBookWindow()
        {
            InitializeComponent();
            _bookService = new GoogleBooksService();
            
            // Yıldızları oluştur
            CreateStars();
        }

        // --- ARAMA İŞLEMLERİ ---
        private void TxtSearch_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                SearchBooks();
            }
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchBooks();
        }

        private async void SearchBooks()
        {
            string searchText = TxtSearch.Text?.Trim() ?? "";
            
            if (string.IsNullOrWhiteSpace(searchText) || searchText.Length < 2)
            {
                System.Windows.MessageBox.Show("Lütfen en az 2 karakter girin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Loading göster
                LoadingBar.Visibility = Visibility.Visible;
                EmptyStatePanel.Visibility = Visibility.Collapsed;
                SearchResultsPanel.Visibility = Visibility.Visible;
                DetailPanel.Visibility = Visibility.Hidden;
                
                // API'den ara
                _searchResults = await _bookService.SearchBooksAsync(searchText);
                
                // Loading gizle
                LoadingBar.Visibility = Visibility.Collapsed;
                
                if (_searchResults.Count > 0)
                {
                    LstBooks.ItemsSource = _searchResults;
                }
                else
                {
                    System.Windows.MessageBox.Show("Arama sonucu bulunamadı. Lütfen farklı bir kitap adı deneyin.", "Sonuç Bulunamadı", MessageBoxButton.OK, MessageBoxImage.Information);
                    EmptyStatePanel.Visibility = Visibility.Visible;
                    SearchResultsPanel.Visibility = Visibility.Collapsed;
                }
            }
            catch (System.Exception ex)
            {
                LoadingBar.Visibility = Visibility.Collapsed;
                
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"Arama hatası: {ex.Message}");
#endif
                
                string errorMessage = "Arama sırasında bir hata oluştu.";
                if (ex is System.Net.Http.HttpRequestException)
                {
                    errorMessage = "İnternet bağlantısı hatası. Lütfen bağlantınızı kontrol edin.";
                }
                
                System.Windows.MessageBox.Show(errorMessage, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- KİTAP SEÇİMİ ---
        private void LstBooks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstBooks.SelectedItem is Book book)
            {
                _selectedBook = book;
                PopulateBookDetails(book);
                DetailPanel.Visibility = Visibility.Visible;
            }
        }

        private void PopulateBookDetails(Book book)
        {
            if (book == null) return;

            try
            {
                // Başlık ve yazar
                TxtTitle.Text = !string.IsNullOrWhiteSpace(book.Title) ? book.Title : "Bilinmeyen Kitap";
                TxtAuthor.Text = !string.IsNullOrWhiteSpace(book.Authors) ? book.Authors : "Bilinmeyen Yazar";
                
                // Publisher ve sayfa sayısı
                TxtCategory.Text = !string.IsNullOrWhiteSpace(book.Publisher) ? book.Publisher : "Genel";
                TxtPageCount.Text = book.PageCount > 0 ? $"{book.PageCount} sayfa" : "Bilinmiyor";
                
                // Açıklama
                TxtDescription.Text = !string.IsNullOrWhiteSpace(book.Description) ? book.Description : "Açıklama bulunamadı.";
                
                // Kapak resmi
                if (!string.IsNullOrEmpty(book.CoverImageUrl) && 
                    !book.CoverImageUrl.StartsWith("/ZImages/") &&
                    System.Uri.TryCreate(book.CoverImageUrl, System.UriKind.Absolute, out System.Uri? imageUri) &&
                    imageUri != null)
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = imageUri;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        ImgCover.ImageSource = bitmap;
                    }
                    catch
                    {
                        // Varsayılan resim kullan
                        ImgCover.ImageSource = new BitmapImage(new System.Uri("/ZImages/defBook.jpg", System.UriKind.Relative));
                    }
                }
                else
                {
                    ImgCover.ImageSource = new BitmapImage(new System.Uri("/ZImages/defBook.jpg", System.UriKind.Relative));
                }
                
                // Okuma ilerlemesi
                TxtTotalPageLabel.Text = book.PageCount > 0 ? $"/ {book.PageCount} Sayfa" : "/ 0 Sayfa";
                TxtCurrentPage.Text = "0";
                UpdateProgressBar();
                
                // Rating'i sıfırla
                _currentRating = 0;
                UpdateStarDisplay(_currentRating);
                TxtStarRating.Text = "0/10";
            }
            catch (System.Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"PopulateBookDetails hatası: {ex.Message}");
#endif
            }
        }

        // --- YILDIZ PUANLAMA ---
        private void CreateStars()
        {
            StarContainer.Children.Clear();
            
            for (int i = 1; i <= StarCount; i++)
            {
                var star = new TextBlock
                {
                    Text = "★",
                    FontSize = 20,
                    Margin = new Thickness(4, 0, 4, 0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139)), // #64748b
                    Tag = i
                };
                
                star.MouseEnter += Star_MouseEnter;
                star.MouseLeave += Star_MouseLeave;
                star.MouseLeftButtonUp += Star_MouseLeftButtonUp;
                
                StarContainer.Children.Add(star);
            }
        }

        private void BtnStarSelect_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            StarPopup.IsOpen = true;
            UpdateStarDisplay(_currentRating);
        }

        private void Star_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is TextBlock star && star.Tag is int value)
            {
                UpdateStarDisplay(value);
            }
        }

        private void Star_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            UpdateStarDisplay(_currentRating);
        }

        private void Star_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is TextBlock star && star.Tag is int value)
            {
                _currentRating = value;
                StarPopup.IsOpen = false;
                UpdateStarDisplay(value);
                TxtStarRating.Text = $"{value}/10";
            }
        }

        private void UpdateStarDisplay(double rating)
        {
            foreach (var child in StarContainer.Children)
            {
                if (child is TextBlock star && star.Tag is int value)
                {
                    bool isFilled = value <= rating;
                    star.Foreground = isFilled
                        ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(251, 191, 36))  // #fbbf24 - gold
                        : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139)); // #64748b - gray
                    star.Opacity = isFilled ? 1.0 : 0.35;
                }
            }
        }

        // --- OKUMA İLERLEMESİ ---
        private void TxtCurrentPage_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateProgressBar();
        }

        private void UpdateProgressBar()
        {
            if (_selectedBook == null || PrgReading == null || TxtPercentage == null) return;

            int currentPage = 0;
            int.TryParse(TxtCurrentPage?.Text, out currentPage);

            int totalPages = _selectedBook.PageCount > 0 ? _selectedBook.PageCount : 100;
            
            // Progress bar'ı güncelle
            PrgReading.Maximum = totalPages;
            PrgReading.Value = System.Math.Min(currentPage, totalPages);

            // Yüzde hesapla ve göster
            double percentage = totalPages > 0 ? (currentPage * 100.0 / totalPages) : 0;
            percentage = System.Math.Min(percentage, 100);
            TxtPercentage.Text = $"{percentage:0}%";
        }

        // --- DURUM DEĞİŞİKLİĞİ ---
        private void CmbStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Durum değiştiğinde yapılacak işlemler (gerekirse)
        }

        // --- KAYDETME ---
        private async void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedBook == null)
            {
                System.Windows.MessageBox.Show("Lütfen önce bir kitap seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Butonu disable et
            BtnAdd.IsEnabled = false;
            BtnAdd.Content = "Kaydediliyor...";

            try
            {
                // Form verilerini modele ekle
                _selectedBook.Status = (CmbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Okunacak";
                _selectedBook.UserRating = _currentRating;
                _selectedBook.CurrentPage = int.TryParse(TxtCurrentPage.Text, out int p) ? p : 0;
                
                // CoverImageUrl kontrolü
                if (string.IsNullOrWhiteSpace(_selectedBook.CoverImageUrl) || 
                    _selectedBook.CoverImageUrl.StartsWith("/ZImages/"))
                {
                    _selectedBook.CoverImageUrl = "/ZImages/defBook.jpg";
                }
                
                // Firebase'e kaydet
                System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                SFirebase firebaseService = new SFirebase();
                string result = await firebaseService.AddBookAsync(_selectedBook);
                System.Windows.Input.Mouse.OverrideCursor = null;

                if (result == "OK")
                {
                    string bookTitle = _selectedBook?.Title ?? "Kitap";
                    this.Close();
                    Helpers.ToastNotification.Show($"{bookTitle} kütüphaneye eklendi!", Helpers.ToastType.Success);
                }
                else
                {
                    System.Windows.MessageBox.Show($"Hata oluştu: {result}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (System.Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"Kitap kaydetme hatası: {ex.Message}");
#endif
                
                string errorMessage = "Kitap kaydedilirken bir hata oluştu.";
                if (ex is System.Net.Http.HttpRequestException)
                {
                    errorMessage = "İnternet bağlantısı hatası. Lütfen bağlantınızı kontrol edin ve tekrar deneyin.";
                }
                
                System.Windows.MessageBox.Show(errorMessage, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnAdd.IsEnabled = true;
                BtnAdd.Content = "+ Kütüphaneme Ekle";
                System.Windows.Input.Mouse.OverrideCursor = null;
            }
        }

        // --- PENCERE KONTROL ---
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Background_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Arka plana tıklanınca pencereyi kapat
            this.Close();
        }
    }
}
