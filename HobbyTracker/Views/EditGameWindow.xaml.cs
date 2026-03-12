using DevExpress.Xpf.Core;
using HobbyTracker.Models;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Linq;
using TextBlockControl = System.Windows.Controls.TextBlock;

namespace HobbyTracker.Views
{
    public partial class EditGameWindow : Window
    {
        // Sabitler - Default image path'leri merkezi yerde tutuluyor
        private const string DefaultCoverImagePath = "pack://application:,,,/ZImages/sample_cover.jpg";
        private const string DefaultCoverImageUrl = "/ZImages/default_cover.jpg";

        // Düzenlenen oyunun referansı
        public Game CurrentGame { get; private set; }

        // Silinme durumu kontrolü
        public bool IsDeleted { get; private set; } = false;

        public EditGameWindow(Game gameToEdit)
        {
            InitializeComponent();

            // 1. Gelen veriyi (veya boşsa yenisini) alıyoruz
            if (gameToEdit == null)
            {
                // Eğer null gelirse (yeni oyun ekleme senaryosu) yeni bir tane oluştur
                CurrentGame = new Game
                {
                    Title = "Yeni Oyun",
                    CoverImageUrl = DefaultCoverImageUrl,
                    AddedDate = System.DateTime.Now
                };
            }
            else
            {
                // Var olan oyunu düzenliyorsak referansını al
                CurrentGame = gameToEdit;
            }

            // 2. KÖPRÜYÜ KURUYORUZ (Binding için en önemli satır)
            // Bu satır sayesinde XAML'daki {Binding Title} gibi kodlar nereye bakacağını bilir.
            this.DataContext = CurrentGame;
            
            // Debug: Genres değerini kontrol et
            System.Diagnostics.Debug.WriteLine($"EditGameWindow açıldı - Genres: '{CurrentGame?.Genres ?? "NULL"}'");
            
            _rawgService = new Services.RAWGService();
            _firebaseService = new Services.SFirebase();
            
            // Favori ikonunu güncelle
            UpdateFavoriteIcon();
        }

        private readonly Services.RAWGService _rawgService;
        private readonly Services.SFirebase _firebaseService;

        // Favori butonu click handler
        private async void BtnFavorite_Click(object sender, MouseButtonEventArgs e)
        {
            if (CurrentGame == null) return;

            CurrentGame.IsFavorite = !CurrentGame.IsFavorite;
            UpdateFavoriteIcon();
            
            // Firebase'e kaydet
            await _firebaseService.UpdateGameAsync(CurrentGame);
        }

        private void UpdateFavoriteIcon()
        {
            if (CurrentGame == null) return;
            TxtFavoriteIcon.Text = CurrentGame.IsFavorite ? "❤️" : "🤍";
            TxtFavoriteLabel.Text = CurrentGame.IsFavorite ? "Favorilerden Çıkar" : "Favorilere Ekle";
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Validasyon: Puan 0-10 arası olmalı
            if (CurrentGame.UserRating < 0 || CurrentGame.UserRating > 10)
            {
                DXMessageBox.Show("Puan 0 ile 10 arasında olmalıdır.", "Geçersiz Değer", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validasyon: Oynama süresi negatif olamaz
            if (CurrentGame.PlayTime < 0)
            {
                DXMessageBox.Show("Oynama süresi negatif olamaz.", "Geçersiz Değer", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Binding kullandığımız için, ekranda kutucuklara yazılan her şey 
            // anında 'CurrentGame' nesnesinin içine işlendi bile.
            // O yüzden burada tekrar "Title = TxtTitle.Text" dememize gerek YOK.

            this.DialogResult = true; // "Tamam" diyerek kapatıyoruz
            this.Close();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            // Custom popup göster
            TxtDeleteMessage.Text = $"\"{CurrentGame?.Title}\" oyununu silmek istediğinize emin misiniz? Bu işlem geri alınamaz.";
            DeletePopupOverlay.Visibility = Visibility.Visible;
        }

        private void BtnCancelDelete_Click(object sender, RoutedEventArgs e)
        {
            DeletePopupOverlay.Visibility = Visibility.Collapsed;
        }

        private void BtnConfirmDelete_Click(object sender, RoutedEventArgs e)
        {
            IsDeleted = true;
            DeletePopupOverlay.Visibility = Visibility.Collapsed;
            this.DialogResult = true;
            this.Close();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            // Vazgeçilirse, değişiklikleri iptal etmiş sayılırız
            this.DialogResult = false;
            this.Close();
        }

        private void Background_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Pencereyi sürükleyebilmek için
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        // Birleştirilmiş ImageFailed handler - kod tekrarını önler
        private void Image_ImageFailed(object sender, System.Windows.ExceptionRoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Image img)
                {
                    img.Source = new BitmapImage(new System.Uri(DefaultCoverImagePath));
                }
            }
            catch { }
        }

        private void ImgCover_Loaded(object sender, RoutedEventArgs e)
        {
            if (CoverLoading != null)
            {
                CoverLoading.Visibility = Visibility.Collapsed;
            }
        }

        // --- Yıldız Puanlama ---
        private List<TextBlockControl> _starBlocks = new List<TextBlockControl>();
        private const int StarCount = 10;
        // Star rating için brush cache - performans iyileştirmesi
        private static readonly System.Windows.Media.SolidColorBrush StarFilledBrush = 
            new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(251, 191, 36)); // #fbbf24
        private static readonly System.Windows.Media.SolidColorBrush StarEmptyBrush = 
            new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139)); // #64748b

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Yıldızları oluştur
            if (StarItems != null && StarItems.Items.Count == 0)
            {
                var stars = Enumerable.Range(1, StarCount).ToList();
                StarItems.ItemsSource = stars;
            }

            // Başlangıçta yıldız görünümünü ve buton etiketini güncelle
            if (CurrentGame != null)
            {
                UpdateStarVisual(CurrentGame.UserRating);
                UpdateStarButtonLabel(CurrentGame.UserRating);

                // Boş alanlar için placeholder mesajları göster (önce)
                UpdateEmptyFieldMessages();
                
                // Eğer tür veya yapımcı bilgisi yoksa ve RawgId varsa çekmeye çalış
                bool needsGenres = string.IsNullOrWhiteSpace(CurrentGame.Genres);
                bool needsDeveloper = string.IsNullOrWhiteSpace(CurrentGame.Developer);
                
                if ((needsGenres || needsDeveloper) && CurrentGame.RawgId > 0)
                {
                    try
                    {
                        var details = await _rawgService.GetGameDetailsAsync(CurrentGame.RawgId);
                        if (details != null)
                        {
                            // Türleri güncelle
                            if (needsGenres && details.Genres != null && details.Genres.Count > 0)
                            {
                                CurrentGame.Genres = string.Join(", ", details.Genres.Select(g => g.Name));
                                // Genres property'si zaten OnPropertyChanged çağırıyor, binding otomatik güncellenecek
                            }
                            
                            // Yapımcıyı güncelle
                            if (needsDeveloper && details.Developers != null && details.Developers.Count > 0)
                            {
                                CurrentGame.Developer = details.Developers.FirstOrDefault()?.Name ?? "";
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                         System.Diagnostics.Debug.WriteLine($"Oyun detayları çekilemedi: {ex.Message}");
                    }
                    
                    // API çağrısından sonra opacity'leri güncelle
                    UpdateEmptyFieldMessages();
                }
            }
        }
        
        private void UpdateEmptyFieldMessages()
        {
            // Yapımcı - sadece opacity ayarla, binding'i bozma
            if (TxtDeveloper != null)
            {
                if (string.IsNullOrWhiteSpace(CurrentGame?.Developer))
                {
                    TxtDeveloper.Opacity = 0.5;
                }
                else
                {
                    TxtDeveloper.Opacity = 1.0;
                }
            }
            
            // Çıkış Tarihi - sadece opacity ayarla, binding'i bozma
            if (TxtReleaseDate != null)
            {
                if (string.IsNullOrWhiteSpace(CurrentGame?.ReleaseDate))
                {
                    TxtReleaseDate.Opacity = 0.5;
                }
                else
                {
                    TxtReleaseDate.Opacity = 1.0;
                }
            }
            
            // Türler - sadece opacity ayarla, binding'i bozma
            if (TxtGenres != null)
            {
                if (string.IsNullOrWhiteSpace(CurrentGame?.Genres))
                {
                    TxtGenres.Opacity = 0.5;
                }
                else
                {
                    TxtGenres.Opacity = 1.0;
                }
            }
        }

        private void BtnStarSelect_Click(object sender, RoutedEventArgs e)
        {
            // Popup aç
            StarPopup.IsOpen = true;
            UpdateStarVisual(CurrentGame.UserRating);
        }

        private void Star_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is TextBlockControl block && block.Tag is int val)
            {
                CurrentGame.UserRating = val;
                StarPopup.IsOpen = false;
                UpdateStarVisual(val);
                UpdateStarButtonLabel(val);
            }
        }

        private void Star_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is TextBlockControl block && block.Tag is int val)
            {
                UpdateStarVisual(val);
            }
        }

        private void Star_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // Hover'dan çıkınca mevcut rating'e dön
            UpdateStarVisual(CurrentGame.UserRating);
        }

        private void UpdateStarVisual(double rating)
        {
            if (StarItems == null) return;
            if (_starBlocks.Count == 0)
            {
                _starBlocks = FindVisualChildren<TextBlockControl>(StarItems).ToList();
            }

            foreach (var star in _starBlocks)
            {
                if (star.Tag is int val)
                {
                    var isFilled = val <= rating;
                    star.Opacity = isFilled ? 1.0 : 0.35;
                    // Cache'lenmiş brush'ları kullan - her seferinde yeni brush oluşturmayı önler
                    star.Foreground = isFilled ? StarFilledBrush : StarEmptyBrush;
                }
            }
        }
        
        private void UpdateStarButtonLabel(double rating)
        {
            if (TxtStarRating != null)
            {
                var clamped = rating;
                if (clamped < 0) clamped = 0;
                if (clamped > StarCount) clamped = StarCount;
                TxtStarRating.Text = $"{clamped:0}/10";
            }
        }

        // Helper: visual tree search
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                if (child is T t) yield return t;
                foreach (var childOfChild in FindVisualChildren<T>(child)) yield return childOfChild;
            }
        }
    }
}