using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace HobbyTracker.Services
{
    /// <summary>
    /// Resim yükleme ve önbellekleme servisi.
    /// Resimleri async olarak yükler ve bellekte önbelleğe alır.
    /// </summary>
    public static class ImageCacheService
    {
        // Thread-safe concurrent dictionary for image caching
        private static readonly ConcurrentDictionary<string, BitmapImage> _memoryCache = new();
        
        // HttpClient instance (reused for all requests)
        private static readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        // Default/fallback images (loaded once)
        private static BitmapImage _defaultBookCover;
        private static BitmapImage _defaultGameCover;

        /// <summary>
        /// Asenkron olarak resim yükler. Önbellekte varsa önbellekten döner.
        /// </summary>
        /// <param name="url">Resim URL'i</param>
        /// <param name="fallbackPath">Hata durumunda kullanılacak yerel resim yolu</param>
        /// <returns>Yüklenen BitmapImage</returns>
        public static async Task<BitmapImage> GetImageAsync(string url, string fallbackPath = null)
        {
            // URL boş veya null ise fallback döndür
            if (string.IsNullOrWhiteSpace(url))
            {
                return GetFallbackImage(fallbackPath);
            }

            // Önbellekte var mı kontrol et
            if (_memoryCache.TryGetValue(url, out var cachedImage))
            {
                return cachedImage;
            }

            try
            {
                // Yerel dosya mı yoksa uzak URL mi kontrol et
                if (url.StartsWith("pack://") || url.StartsWith("/") || File.Exists(url))
                {
                    return await LoadLocalImageAsync(url, fallbackPath);
                }
                else
                {
                    return await LoadRemoteImageAsync(url, fallbackPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ImageCacheService: Resim yüklenemedi: {url}, Hata: {ex.Message}");
                return GetFallbackImage(fallbackPath);
            }
        }

        /// <summary>
        /// Yerel resim dosyasını async yükler
        /// </summary>
        private static async Task<BitmapImage> LoadLocalImageAsync(string path, string fallbackPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    
                    if (path.StartsWith("pack://"))
                    {
                        bitmap.UriSource = new Uri(path);
                    }
                    else if (path.StartsWith("/"))
                    {
                        bitmap.UriSource = new Uri($"pack://application:,,,{path}");
                    }
                    else
                    {
                        bitmap.UriSource = new Uri(path, UriKind.Absolute);
                    }
                    
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze(); // Cross-thread erişim için freeze et
                    
                    // Önbelleğe ekle
                    _memoryCache.TryAdd(path, bitmap);
                    
                    return bitmap;
                }
                catch
                {
                    return null;
                }
            }) ?? GetFallbackImage(fallbackPath);
        }

        /// <summary>
        /// Uzak URL'den resmi async yükler
        /// </summary>
        private static async Task<BitmapImage> LoadRemoteImageAsync(string url, string fallbackPath)
        {
            try
            {
                // HTTP isteği ile byte array olarak indir
                var imageBytes = await _httpClient.GetByteArrayAsync(url);
                
                // UI thread'den bağımsız olarak BitmapImage oluştur
                var bitmap = await Task.Run(() =>
                {
                    using var stream = new MemoryStream(imageBytes);
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = stream;
                    bmp.EndInit();
                    bmp.Freeze(); // Cross-thread erişim için freeze et
                    return bmp;
                });
                
                // Önbelleğe ekle
                _memoryCache.TryAdd(url, bitmap);
                
                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ImageCacheService: Uzak resim yüklenemedi: {url}, Hata: {ex.Message}");
                return GetFallbackImage(fallbackPath);
            }
        }

        /// <summary>
        /// Fallback resim döndürür
        /// </summary>
        private static BitmapImage GetFallbackImage(string fallbackPath)
        {
            if (string.IsNullOrEmpty(fallbackPath))
            {
                fallbackPath = "pack://application:,,,/ZImages/defBook.jpg";
            }
            
            // Fallback resimlerini önbellekten al veya oluştur
            if (fallbackPath.Contains("defBook") || fallbackPath.Contains("sample_book"))
            {
                if (_defaultBookCover == null)
                {
                    _defaultBookCover = CreateFallbackBitmap("pack://application:,,,/ZImages/defBook.jpg");
                }
                return _defaultBookCover;
            }
            else if (fallbackPath.Contains("sample_cover") || fallbackPath.Contains("game"))
            {
                if (_defaultGameCover == null)
                {
                    _defaultGameCover = CreateFallbackBitmap("pack://application:,,,/ZImages/sample_cover.jpg");
                }
                return _defaultGameCover;
            }
            
            return CreateFallbackBitmap(fallbackPath);
        }

        /// <summary>
        /// Fallback bitmap oluşturur
        /// </summary>
        private static BitmapImage CreateFallbackBitmap(string path)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Önbelleği temizler
        /// </summary>
        public static void ClearCache()
        {
            _memoryCache.Clear();
        }

        /// <summary>
        /// Belirli bir URL'i önbellekten siler
        /// </summary>
        public static void RemoveFromCache(string url)
        {
            _memoryCache.TryRemove(url, out _);
        }

        /// <summary>
        /// Önbellekteki resim sayısını döndürür
        /// </summary>
        public static int CacheCount => _memoryCache.Count;
    }
}
