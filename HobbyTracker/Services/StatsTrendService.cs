using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace HobbyTracker.Services
{
    /// <summary>
    /// İstatistik trend değerlerini haftalık olarak takip eden servis.
    /// Her hafta başında önceki haftanın değerleri referans olarak kaydedilir.
    /// Trend göstergeleri her zaman haftalık referans değerleriyle karşılaştırılır.
    /// </summary>
    public class StatsTrendService
    {
        private static readonly string CacheFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HobbyTracker",
            "stats_weekly_cache.json"
        );

        private WeeklyStatsCache _cache;

        public StatsTrendService()
        {
            _cache = LoadCache();
            CheckAndUpdateWeek();
        }

        /// <summary>
        /// Hafta kontrolü yapar. Yeni bir haftaya geçildiyse referans değerlerini günceller.
        /// </summary>
        private void CheckAndUpdateWeek()
        {
            int currentWeek = GetWeekOfYear(DateTime.Now);
            int currentYear = DateTime.Now.Year;

            // Yeni hafta veya yeni yıl mı?
            if (_cache.WeekNumber != currentWeek || _cache.Year != currentYear)
            {
                // Mevcut değerleri haftalık referans olarak kaydet
                _cache.WeeklyReferenceStats = new Dictionary<string, double>(_cache.CurrentStats);
                _cache.WeekNumber = currentWeek;
                _cache.Year = currentYear;
                SaveToFile();
            }
        }

        /// <summary>
        /// ISO 8601 hafta numarasını hesaplar
        /// </summary>
        private int GetWeekOfYear(DateTime date)
        {
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            return culture.Calendar.GetWeekOfYear(date, 
                System.Globalization.CalendarWeekRule.FirstFourDayWeek, 
                DayOfWeek.Monday);
        }

        /// <summary>
        /// Trend yönünü hesaplar (haftalık referansa göre)
        /// </summary>
        /// <param name="key">Stat anahtarı (örn: "books_total", "movies_watched")</param>
        /// <param name="currentValue">Mevcut değer</param>
        /// <returns>Trend yönü: 1 = artış, -1 = azalma, 0 = değişim yok</returns>
        public int GetTrend(string key, double currentValue)
        {
            // Haftalık referans değeriyle karşılaştır
            if (_cache.WeeklyReferenceStats.TryGetValue(key, out double referenceValue))
            {
                if (currentValue > referenceValue) return 1;  // Artış
                if (currentValue < referenceValue) return -1; // Azalma
                return 0; // Değişim yok
            }
            
            // İlk kez veya referans yok - varsayılan olarak artış göster
            return 1;
        }

        /// <summary>
        /// Trend ikonunu döndürür
        /// </summary>
        public string GetTrendIcon(string key, double currentValue)
        {
            int trend = GetTrend(key, currentValue);
            return trend switch
            {
                1 => "↗",  // Artış
                -1 => "↘", // Azalma
                _ => "→"   // Değişim yok
            };
        }

        /// <summary>
        /// Trend rengini döndürür (hex)
        /// </summary>
        public string GetTrendColor(string key, double currentValue, string defaultColor)
        {
            int trend = GetTrend(key, currentValue);
            return trend switch
            {
                1 => "#10b981",  // Yeşil - Artış
                -1 => "#ef4444", // Kırmızı - Azalma
                _ => "#6b7280"   // Gri - Değişim yok
            };
        }

        /// <summary>
        /// Mevcut değeri kaydeder (UI güncelleme için kullanılır, referansı değiştirmez)
        /// </summary>
        public void SaveValue(string key, double value)
        {
            _cache.CurrentStats[key] = value;
        }

        /// <summary>
        /// Tüm değişiklikleri dosyaya yazar
        /// </summary>
        public void SaveToFile()
        {
            try
            {
                string directory = Path.GetDirectoryName(CacheFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonSerializer.Serialize(_cache, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(CacheFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Stats cache kaydetme hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Önbelleği dosyadan yükler
        /// </summary>
        private WeeklyStatsCache LoadCache()
        {
            try
            {
                if (File.Exists(CacheFilePath))
                {
                    string json = File.ReadAllText(CacheFilePath);
                    return JsonSerializer.Deserialize<WeeklyStatsCache>(json) 
                           ?? CreateNewCache();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Stats cache yükleme hatası: {ex.Message}");
            }

            return CreateNewCache();
        }

        private WeeklyStatsCache CreateNewCache()
        {
            return new WeeklyStatsCache
            {
                WeekNumber = GetWeekOfYear(DateTime.Now),
                Year = DateTime.Now.Year,
                CurrentStats = new Dictionary<string, double>(),
                WeeklyReferenceStats = new Dictionary<string, double>()
            };
        }

        // Stat Anahtarları (Sabitler)
        public static class Keys
        {
            // Books
            public const string BooksTotal = "books_total";
            public const string BooksRead = "books_read";
            public const string BooksReading = "books_reading"; // Yeni
            public const string BooksPages = "books_pages";
            public const string BooksRating = "books_rating";

            // Games
            public const string GamesTotal = "games_total";
            public const string GamesCompleted = "games_completed";
            public const string GamesPlaying = "games_playing";
            public const string GamesPlayTime = "games_playtime";
            public const string GamesRating = "games_rating";

            // Series
            public const string SeriesTotal = "series_total";
            public const string SeriesWatched = "series_watched";
            public const string SeriesWatching = "series_watching"; // Yeni
            public const string SeriesEpisodes = "series_episodes";
            public const string SeriesRating = "series_rating";

            // Movies
            public const string MoviesTotal = "movies_total";
            public const string MoviesWatched = "movies_watched";
            public const string MoviesWatching = "movies_watching"; // Yeni
            public const string MoviesWatchTime = "movies_watchtime";
            public const string MoviesRating = "movies_rating";
        }
    }

    /// <summary>
    /// Haftalık istatistik cache yapısı
    /// </summary>
    public class WeeklyStatsCache
    {
        public int WeekNumber { get; set; }
        public int Year { get; set; }
        public Dictionary<string, double> CurrentStats { get; set; } = new();
        public Dictionary<string, double> WeeklyReferenceStats { get; set; } = new();
    }
}
