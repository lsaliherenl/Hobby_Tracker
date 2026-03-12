using System;
using System.Collections.Generic;

namespace HobbyTracker.Models
{
    /// <summary>
    /// Kullanıcı istatistikleri modeli. Tüm hobi kategorilerindeki toplam verileri içerir.
    /// </summary>
    public class UserStats
    {
        /// <summary>
        /// Kütüphanedeki toplam oyun sayısı.
        /// </summary>
        public int TotalGames { get; set; }
        
        /// <summary>
        /// Bitirilen oyun sayısı.
        /// </summary>
        public int FinishedGames { get; set; }
        
        /// <summary>
        /// Toplam oynama süresi (saat).
        /// </summary>
        public int TotalGameHours { get; set; }
        
        /// <summary>
        /// En sevilen oyun türü.
        /// </summary>
        public string FavoriteGenre { get; set; } = string.Empty;

        /// <summary>
        /// Kütüphanedeki toplam kitap sayısı.
        /// </summary>
        public int TotalBooks { get; set; }
        
        /// <summary>
        /// Okunan kitap sayısı.
        /// </summary>
        public int ReadBooks { get; set; }
        
        /// <summary>
        /// Toplam okunan sayfa sayısı.
        /// </summary>
        public int TotalPagesRead { get; set; }
        
        /// <summary>
        /// Yıllık okuma hedefi. Varsayılan: 20.
        /// </summary>
        public int YearlyGoal { get; set; } = 20;

        /// <summary>
        /// Listedeki toplam film sayısı.
        /// </summary>
        public int TotalMovies { get; set; }
        
        /// <summary>
        /// İzlenen film sayısı.
        /// </summary>
        public int WatchedMovies { get; set; }
        
        /// <summary>
        /// Toplam izlenen film süresi (dakika).
        /// </summary>
        public int TotalMovieMinutes { get; set; }

        /// <summary>
        /// Takip edilen dizi sayısı.
        /// </summary>
        public int TotalSeries { get; set; }
        
        /// <summary>
        /// Toplam izlenen bölüm sayısı.
        /// </summary>
        public int TotalEpisodesWatched { get; set; }
        
        /// <summary>
        /// Toplam izlenen dizi süresi (dakika).
        /// </summary>
        public int TotalSeriesMinutes { get; set; }

        /// <summary>
        /// Mevcut kullanıcı seviyesi. Varsayılan: 1.
        /// </summary>
        public int CurrentLevel { get; set; } = 1;
        
        /// <summary>
        /// Mevcut deneyim puanı. Varsayılan: 0.
        /// </summary>
        public int CurrentXP { get; set; } = 0;
        
        /// <summary>
        /// Sonraki seviye için gereken deneyim puanı. Varsayılan: 1000.
        /// </summary>
        public int NextLevelXP { get; set; } = 1000;
        
        /// <summary>
        /// Kullanıcı unvanı. Varsayılan: "Acemi".
        /// </summary>
        public string UserTitle { get; set; } = "Acemi";

        /// <summary>
        /// Ardışık aktivite günü sayısı (streak).
        /// </summary>
        public int ActivityStreak { get; set; }
        
        /// <summary>
        /// Son aktivite tarihi.
        /// </summary>
        public DateTime LastActivityDate { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Toplam izleme süresi (saat). Film ve dizi sürelerinin toplamı.
        /// </summary>
        public int TotalWatchHours => (TotalMovieMinutes + TotalSeriesMinutes) / 60;
        
        /// <summary>
        /// Tüm kategorilerdeki toplam içerik sayısı.
        /// </summary>
        public int TotalItems => TotalGames + TotalBooks + TotalMovies + TotalSeries;

        /// <summary>
        /// Haftalık aktivite verisi. Grafik için kullanılır.
        /// </summary>
        public List<DailyActivity> WeeklyActivity { get; set; } = new List<DailyActivity>();

        /// <summary>
        /// Toplam harcanan zamanı TimeSpan olarak döndürür.
        /// </summary>
        /// <returns>Oyun, film ve dizi sürelerinin toplamı.</returns>
        public TimeSpan GetTotalTimeSpent()
        {
            double totalMinutes = (TotalGameHours * 60) + TotalMovieMinutes + TotalSeriesMinutes;
            return TimeSpan.FromMinutes(totalMinutes);
        }

        /// <summary>
        /// Toplam harcanan süreyi saat cinsinden döndürür.
        /// </summary>
        /// <returns>Oyun ve izleme sürelerinin toplamı (saat).</returns>
        public int GetTotalHoursSpent()
        {
            return TotalGameHours + TotalWatchHours;
        }
    }

    /// <summary>
    /// Günlük aktivite verisi. Haftalık grafik için kullanılır.
    /// </summary>
    public class DailyActivity
    {
        /// <summary>
        /// Aktivite tarihi.
        /// </summary>
        public DateTime Date { get; set; }
        
        /// <summary>
        /// Gün adı kısaltması (Pzt, Sal, Çar, vb.).
        /// </summary>
        public string DayName { get; set; } = string.Empty;
        
        /// <summary>
        /// O gün yapılan aktivite sayısı.
        /// </summary>
        public int ActivityCount { get; set; }
    }
}