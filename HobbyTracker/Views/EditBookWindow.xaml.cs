using DevExpress.Xpf.Core;
using HobbyTracker.Models;
using HobbyTracker.Services;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HobbyTracker.Views
{
    public partial class EditBookWindow : Window
    {
        public Book CurrentBook { get; private set; }
        private SFirebase _firebaseService;
        public bool IsDeleted { get; private set; } = false;

        public EditBookWindow(Book bookToEdit)
        {
            InitializeComponent();
            _firebaseService = new SFirebase();

            // Referans al
            if (bookToEdit != null)
            {
                CurrentBook = bookToEdit;
            }
            else
            {
                CurrentBook = new Book { Title = "Yeni Kitap" };
            }

            // Geriye dönük uyumluluk: "Bitti" durumunu "Okundu" olarak göster ve kaydet
            if (CurrentBook.Status == "Bitti")
            {
                CurrentBook.Status = "Okundu";
            }

            this.DataContext = CurrentBook;
            
            // Favori ikonunu güncelle
            UpdateFavoriteIcon();
        }

        // Favori butonu click handler
        private async void BtnFavorite_Click(object sender, MouseButtonEventArgs e)
        {
            if (CurrentBook == null) return;

            CurrentBook.IsFavorite = !CurrentBook.IsFavorite;
            UpdateFavoriteIcon();
            
            // Firebase'e kaydet
            await _firebaseService.UpdateBookAsync(CurrentBook);
        }

        private void UpdateFavoriteIcon()
        {
            if (CurrentBook == null) return;
            TxtFavoriteIcon.Text = CurrentBook.IsFavorite ? "❤️" : "🤍";
            TxtFavoriteLabel.Text = CurrentBook.IsFavorite ? "Favorilerden Çıkar" : "Favorilere Ekle";
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Validasyon
            if (CurrentBook.UserRating < 0 || CurrentBook.UserRating > 10)
            {
                DXMessageBox.Show("Puan 0-10 arasında olmalıdır.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (CurrentBook.CurrentPage < 0 || CurrentBook.CurrentPage > CurrentBook.PageCount)
            {
                DXMessageBox.Show($"Okunan sayfa 0 ile {CurrentBook.PageCount} arasında olmalıdır.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Firebase Güncelleme
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            try 
            {
                await _firebaseService.UpdateBookAsync(CurrentBook);
                string bookTitle = CurrentBook?.Title ?? "Kitap";
                this.DialogResult = true;
                this.Close();
                Helpers.ToastNotification.Show($"{bookTitle} başarıyla güncellendi!", Helpers.ToastType.Success);
            }
            catch (System.Exception ex)
            {
                DXMessageBox.Show("Kaydedilirken hata oluştu: " + ex.Message);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            // Custom popup göster
            TxtDeleteMessage.Text = $"\"{CurrentBook?.Title}\" kitabını silmek istediğinize emin misiniz? Bu işlem geri alınamaz.";
            DeletePopupOverlay.Visibility = Visibility.Visible;
        }

        private void BtnCancelDelete_Click(object sender, RoutedEventArgs e)
        {
            DeletePopupOverlay.Visibility = Visibility.Collapsed;
        }

        private async void BtnConfirmDelete_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            try
            {
                if (!string.IsNullOrEmpty(CurrentBook.Id))
                {
                    await _firebaseService.DeleteBookAsync(CurrentBook.Id);
                }
                IsDeleted = true;
                DeletePopupOverlay.Visibility = Visibility.Collapsed;
                this.DialogResult = true;
                this.Close();
            }
            catch (System.Exception ex)
            {
                DeletePopupOverlay.Visibility = Visibility.Collapsed;
                DXMessageBox.Show("Silinirken hata oluştu: " + ex.Message);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Background_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        // --- Görsel Yükleme Hataları ---
        private void ImgCover_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Image img) img.Source = new BitmapImage(new System.Uri("pack://application:,,,/ZImages/sample_book_cover.jpg"));
        }

        private void BgImage_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Image img) img.Source = new BitmapImage(new System.Uri("pack://application:,,,/ZImages/sample_book_cover.jpg"));
        }

        // --- Yıldız Sistemi ---
        private List<TextBlock> _starBlocks = new List<TextBlock>();
        private const int StarCount = 10;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (StarItems != null && StarItems.Items.Count == 0)
            {
                StarItems.ItemsSource = Enumerable.Range(1, StarCount).ToList();
            }
            UpdateStarVisual(CurrentBook.UserRating);
            UpdateStarButtonLabel(CurrentBook.UserRating);
        }

        private void BtnStarSelect_Click(object sender, RoutedEventArgs e)
        {
            StarPopup.IsOpen = true;
            UpdateStarVisual(CurrentBook.UserRating);
        }

        private void Star_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock block && block.Tag is int val)
            {
                CurrentBook.UserRating = val;
                StarPopup.IsOpen = false;
                UpdateStarVisual(val);
                UpdateStarButtonLabel(val);
            }
        }

        private void Star_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is TextBlock block && block.Tag is int val) UpdateStarVisual(val);
        }

        private void Star_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            UpdateStarVisual(CurrentBook.UserRating);
        }

        private void UpdateStarVisual(double rating)
        {
            if (StarItems == null) return;
            if (_starBlocks.Count == 0) _starBlocks = FindVisualChildren<TextBlock>(StarItems).ToList();

            foreach (var star in _starBlocks)
            {
                if (star.Tag is int val)
                {
                    bool isFilled = val <= rating;
                    star.Opacity = isFilled ? 1.0 : 0.35;
                    star.Foreground = isFilled ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(251, 191, 36)) : new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139));
                }
            }
        }

        private void UpdateStarButtonLabel(double rating)
        {
            if (TxtStarRating != null)
            {
                double clamped = System.Math.Clamp(rating, 0, 10);
                TxtStarRating.Text = $"{clamped:0}/10";
            }
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                if (child is T t) yield return t;
                foreach (var childOfChild in FindVisualChildren<T>(child)) yield return childOfChild;
            }
        }
    }
}
