using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace HobbyTracker.Models
{
    /// <summary>
    /// Dizi/Anime modeli. TMDB API'den gelen veriler ve kullanıcı izleme bilgilerini içerir.
    /// </summary>
    public class Series : HobbyItem
    {
        /// <summary>
        /// TMDB API'den gelen benzersiz dizi kimliği.
        /// </summary>
        public int TmdbId { get; set; }
        
        /// <summary>
        /// Dizinin orijinal adı.
        /// </summary>
        public string OriginalName { get; set; } = string.Empty;
        
        /// <summary>
        /// Dizi özeti.
        /// </summary>
        public string Overview { get; set; } = string.Empty;
        
        /// <summary>
        /// İlk yayın tarihi.
        /// </summary>
        public string FirstAirDate { get; set; } = string.Empty;
        
        /// <summary>
        /// Dizi türleri (virgülle ayrılmış).
        /// </summary>
        public string Genres { get; set; } = string.Empty;
        
        /// <summary>
        /// Yayın platformu (Netflix, HBO, vb.).
        /// </summary>
        public string Network { get; set; } = string.Empty;
        
        /// <summary>
        /// Orijinal dil kodu.
        /// </summary>
        public string OriginalLanguage { get; set; } = string.Empty;
        
        /// <summary>
        /// TMDB puanı.
        /// </summary>
        public double TmdbRating { get; set; }

        /// <summary>
        /// Toplam sezon sayısı.
        /// </summary>
        public int TotalSeasons { get; set; }
        
        /// <summary>
        /// Toplam bölüm sayısı.
        /// </summary>
        public int TotalEpisodes { get; set; }
        
        /// <summary>
        /// Ortalama bölüm süresi (dakika).
        /// </summary>
        public int AvgEpisodeRuntime { get; set; }

        /// <summary>
        /// Arka plan resmi URL'i (yatay format).
        /// </summary>
        public string BackdropUrl { get; set; } = string.Empty;

        /// <summary>
        /// Kullanıcının izleme durumu.
        /// </summary>
        public WatchStatus UserStatus { get; set; } = WatchStatus.PlanToWatch;
        
        /// <summary>
        /// Kullanıcının verdiği puan (0-10 arası).
        /// </summary>
        public double UserRating { get; set; }
        
        /// <summary>
        /// Kullanıcının kaldığı sezon numarası.
        /// </summary>
        public int MyCurrentSeason { get; set; }
        
        /// <summary>
        /// Kullanıcının kaldığı bölüm numarası.
        /// </summary>
        public int MyCurrentEpisode { get; set; }
        
        /// <summary>
        /// İzlenme tarihi.
        /// </summary>
        public DateTime? WatchedDate { get; set; }
        
        /// <summary>
        /// Kullanıcının eklediği kişisel notlar.
        /// </summary>
        public string PersonalNote { get; set; } = string.Empty;
        
        /// <summary>
        /// Kullanıcının eklediği özel etiketler.
        /// </summary>
        public List<string> UserTags { get; set; } = new List<string>();
        
        /// <summary>
        /// İzlenen bölümler. Format: "sezon_bölüm" (örn: "1_1", "2_5").
        /// O(1) lookup performansı için HashSet kullanılır.
        /// </summary>
        public HashSet<string> WatchedEpisodes { get; set; } = new HashSet<string>();

        /// <summary>
        /// Oyuncu kadrosu.
        /// </summary>
        public List<CastMember> Cast { get; set; } = new List<CastMember>();

        /// <summary>
        /// Çıkış yılı. FirstAirDate'den ilk 4 karakteri alır.
        /// </summary>
        [JsonIgnore]
        public string Year => !string.IsNullOrEmpty(FirstAirDate) && FirstAirDate.Length >= 4
                              ? FirstAirDate.Substring(0, 4) : "N/A";

        /// <summary>
        /// İlerleme metni formatı (örn: "S2 B5").
        /// </summary>
        [JsonIgnore]
        public string ProgressText => $"S{MyCurrentSeason} B{MyCurrentEpisode}";
        
        /// <summary>
        /// İzlenen toplam bölüm sayısı.
        /// </summary>
        [JsonIgnore]
        public int WatchedEpisodesCount => WatchedEpisodes?.Count ?? 0;

        /// <summary>
        /// İzlenen toplam süre (dakika). İzlenen bölüm sayısı × Ortalama bölüm süresi.
        /// </summary>
        [JsonIgnore]
        public int TotalWatchedMinutes => WatchedEpisodesCount * (AvgEpisodeRuntime > 0 ? AvgEpisodeRuntime : 45);

        /// <summary>
        /// İzleme ilerlemesi yüzdesi (0-100). Progress Bar için kullanılır.
        /// </summary>
        [JsonIgnore]
        public int ProgressPercentage
        {
            get
            {
                if (UserStatus == WatchStatus.Completed) return 100;
                if (TotalEpisodes <= 0) return 0;
                return (int)((double)WatchedEpisodesCount / TotalEpisodes * 100);
            }
        }

        /// <summary>
        /// Durum gösterim metni. Kartlarda kullanılır.
        /// </summary>
        [JsonIgnore]
        public string StatusDisplay => UserStatus switch
        {
            WatchStatus.Completed => "İzlendi",
            WatchStatus.InProgress => "İzleniyor",
            WatchStatus.PlanToWatch => "Listem",
            WatchStatus.Dropped => "Yarım Bırakıldı",
            _ => "Belirsiz"
        };

        /// <summary>
        /// Durum rozeti arka plan rengi.
        /// </summary>
        [JsonIgnore]
        public string StatusBadgeBackground => UserStatus switch
        {
            WatchStatus.Completed => "#EF4444",
            WatchStatus.InProgress => "#3B82F6",
            WatchStatus.PlanToWatch => "#22C55E",
            WatchStatus.Dropped => "#6B7280",
            _ => "#6B7280"
        };

        /// <summary>
        /// Kalan bölüm sayısı metni.
        /// </summary>
        [JsonIgnore]
        public string RemainingText
        {
            get
            {
                if (UserStatus == WatchStatus.Completed) return "Tamamlandı";
                int remaining = TotalEpisodes - WatchedEpisodesCount;
                return remaining > 0 ? $"{remaining} bölüm kaldı" : "Tamamlamak üzere";
            }
        }
        
        /// <summary>
        /// Belirtilen bölümün izlenip izlenmediğini kontrol eder. O(1) performans.
        /// </summary>
        /// <param name="season">Sezon numarası.</param>
        /// <param name="episode">Bölüm numarası.</param>
        /// <returns>Bölüm izlendiyse true, aksi halde false.</returns>
        public bool IsEpisodeWatched(int season, int episode)
        {
            return WatchedEpisodes?.Contains($"{season}_{episode}") ?? false;
        }
        
        /// <summary>
        /// Bölümün izlenme durumunu değiştirir (toggle).
        /// </summary>
        /// <param name="season">Sezon numarası.</param>
        /// <param name="episode">Bölüm numarası.</param>
        public void ToggleEpisodeWatched(int season, int episode)
        {
            if (WatchedEpisodes == null)
                WatchedEpisodes = new HashSet<string>();
            
            string key = $"{season}_{episode}";
            if (WatchedEpisodes.Contains(key))
                WatchedEpisodes.Remove(key);
            else
                WatchedEpisodes.Add(key);
        }
        
        /// <summary>
        /// Belirtilen sezonda izlenen bölüm sayısını döndürür.
        /// </summary>
        /// <param name="seasonNumber">Sezon numarası.</param>
        /// <returns>İzlenen bölüm sayısı.</returns>
        public int GetWatchedCountForSeason(int seasonNumber)
        {
            if (WatchedEpisodes == null) return 0;
            string prefix = $"{seasonNumber}_";
            return WatchedEpisodes.Count(ep => ep.StartsWith(prefix));
        }
        
        /// <summary>
        /// Belirtilen sezonun tüm bölümlerini izlendi olarak işaretler.
        /// </summary>
        /// <param name="seasonNumber">Sezon numarası.</param>
        /// <param name="episodeCount">Sezondaki toplam bölüm sayısı.</param>
        public void MarkSeasonWatched(int seasonNumber, int episodeCount)
        {
            if (WatchedEpisodes == null)
                WatchedEpisodes = new HashSet<string>();
            
            for (int ep = 1; ep <= episodeCount; ep++)
            {
                WatchedEpisodes.Add($"{seasonNumber}_{ep}");
            }
        }
        
        /// <summary>
        /// Belirtilen sezonun tüm bölümlerinin izlenme durumunu kaldırır.
        /// </summary>
        /// <param name="seasonNumber">Sezon numarası.</param>
        public void UnmarkSeasonWatched(int seasonNumber)
        {
            if (WatchedEpisodes == null) return;
            
            WatchedEpisodes.RemoveWhere(ep => ep.StartsWith($"{seasonNumber}_"));
        }
    }
}