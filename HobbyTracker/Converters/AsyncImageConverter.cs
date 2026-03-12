using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace HobbyTracker.Converters
{
    /// <summary>
    /// URL'den resim yükleyen value converter. XAML binding'lerde kullanılır.
    /// </summary>
    public class AsyncImageConverter : IValueConverter
    {
        /// <summary>
        /// Varsayılan kitap kapak resmi. Uygulama başında bir kez yüklenir.
        /// </summary>
        private static readonly BitmapImage? _defaultBook;
        
        /// <summary>
        /// Varsayılan oyun kapak resmi. Uygulama başında bir kez yüklenir.
        /// </summary>
        private static readonly BitmapImage? _defaultGame;

        static AsyncImageConverter()
        {
            try
            {
                _defaultBook = new BitmapImage();
                _defaultBook.BeginInit();
                _defaultBook.UriSource = new Uri("pack://application:,,,/ZImages/defBook.jpg");
                _defaultBook.CacheOption = BitmapCacheOption.OnLoad;
                _defaultBook.EndInit();
                _defaultBook.Freeze();
            }
            catch { }

            try
            {
                _defaultGame = new BitmapImage();
                _defaultGame.BeginInit();
                _defaultGame.UriSource = new Uri("pack://application:,,,/ZImages/sample_cover.jpg");
                _defaultGame.CacheOption = BitmapCacheOption.OnLoad;
                _defaultGame.EndInit();
                _defaultGame.Freeze();
            }
            catch { }
        }

        /// <summary>
        /// String URL'i BitmapImage'e dönüştürür.
        /// </summary>
        /// <param name="value">Resim URL'i.</param>
        /// <param name="targetType">Hedef tip.</param>
        /// <param name="parameter">Fallback tipi ("book" veya "game").</param>
        /// <param name="culture">Kültür bilgisi.</param>
        /// <returns>BitmapImage nesnesi veya varsayılan resim.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var url = value as string;
            var fallbackType = parameter as string ?? "book";
            var defaultImage = fallbackType.ToLower() == "game" ? _defaultGame : _defaultBook;

            if (string.IsNullOrWhiteSpace(url))
            {
                return defaultImage ?? new BitmapImage();
            }

            try
            {
                if (url.StartsWith("pack://") || url.StartsWith("/"))
                {
                    var localUri = url.StartsWith("/") 
                        ? new Uri($"pack://application:,,,{url}") 
                        : new Uri(url);
                    
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = localUri;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }

                if (Uri.TryCreate(url, UriKind.Absolute, out Uri? validUri))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = validUri;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.DecodePixelWidth = 400;
                    bitmap.EndInit();
                    return bitmap;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AsyncImageConverter Error: {ex.Message}");
            }

            return defaultImage ?? new BitmapImage();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
