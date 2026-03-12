using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using HobbyTracker.Models;
using Newtonsoft.Json.Linq; // JSON işlemleri için

namespace HobbyTracker.Services
{
    public class TmdbService
    {
        // ==========================================
        // API AYARLARI
        // ==========================================
        // API Key artık secrets.json'dan okunuyor
        private static string ApiKey => HobbyTracker.Helpers.ConfigurationHelper.GetTmdbApiKey();

        private const string BaseUrl = "https://api.themoviedb.org/3";
        private const string ImageBaseUrl = "https://image.tmdb.org/t/p/w500";     // Posterler için
        private const string BackdropBaseUrl = "https://image.tmdb.org/t/p/original"; // Arka Planlar için (Kaliteli olsun)

        private readonly HttpClient _httpClient;

        public TmdbService()
        {
            _httpClient = new HttpClient();
        }

        // ==========================================
        // 1. FİLM ARAMA (MOVIES)
        // ==========================================
        public async Task<List<Movie>> SearchMoviesAsync(string query)
        {
            var movies = new List<Movie>();

            if (string.IsNullOrWhiteSpace(query)) return movies;

            try
            {
                // Sadece Filmleri Ara (language=tr-TR)
                string url = $"{BaseUrl}/search/movie?api_key={ApiKey}&query={query}&language=tr-TR&include_adult=false";
                var json = await GetJsonAsync(url);

                if (json["results"] != null)
                {
                    foreach (var item in json["results"])
                    {
                        // --- YENİ MODEL EŞLEŞTİRMESİ ---
                        var movie = new Movie
                        {
                            // 1. ID (Artık string tutuyoruz)
                            Id = item["id"]?.ToString(),

                            // 2. Başlık
                            Title = item["title"]?.ToString(),

                            // 3. Özet (Eski: Overview -> Yeni: Synopsis)
                            Synopsis = item["overview"]?.ToString(),

                            // 4. Puan (Eski: TmdbRating -> Yeni: GlobalRating)
                            GlobalRating = item["vote_average"] != null ? (double)item["vote_average"] : 0,

                            // 5. Orijinal Dil
                            OriginalLanguage = item["original_language"]?.ToString(),

                            // 6. Poster (Eski: CoverImageUrl -> Yeni: PosterUrl)
                            PosterUrl = item["poster_path"] != null
                                ? ImageBaseUrl + item["poster_path"]
                                : "/Assets/Images/default_movie.png", // Projendeki varsayılan resim yolu

                            // 7. Arka Plan
                            BackdropUrl = item["backdrop_path"] != null
                                ? BackdropBaseUrl + item["backdrop_path"]
                                : null,

                            // 8. Durum (Varsayılan olarak "Planlandı" atanır)
                            UserStatus = WatchStatus.PlanToWatch,

                            // 9. Listeleri Boş Başlat (Null hatası almamak için)
                            Genres = new List<string>(),
                            Cast = new List<CastMember>(),
                            UserTags = new List<string>()
                        };

                        // 10. Tarih İşlemi (Sadece Yıl lazım)
                        if (item["release_date"] != null && DateTime.TryParse(item["release_date"].ToString(), out DateTime date))
                        {
                            movie.Year = date.Year;
                        }
                        else
                        {
                            movie.Year = 0;
                        }

                        movies.Add(movie);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Film Arama Hatası: {ex.Message}");
            }

            return movies;
        }

        // ==========================================
        // 2. DİZİ ARAMA (SERIES)
        // ==========================================
        // NOT: Eğer Series modelini de Movie gibi güncellediysen burayı da ona benzetmelisin.
        // Şimdilik eski Series yapısına göre bıraktım.
        public async Task<List<Series>> SearchSeriesAsync(string query)
        {
            var seriesList = new List<Series>();

            if (string.IsNullOrWhiteSpace(query)) return seriesList;

            try
            {
                string url = $"{BaseUrl}/search/tv?api_key={ApiKey}&query={query}&language=tr-TR&include_adult=false";
                var json = await GetJsonAsync(url);

                if (json["results"] != null)
                {
                    foreach (var item in json["results"])
                    {
                        var series = new Series
                        {
                            TmdbId = (int)item["id"],
                            Title = item["name"]?.ToString(),
                            OriginalName = item["original_name"]?.ToString(),
                            Overview = item["overview"]?.ToString(),
                            FirstAirDate = item["first_air_date"]?.ToString(),
                            TmdbRating = item["vote_average"] != null ? (double)item["vote_average"] : 0,

                            CoverImageUrl = item["poster_path"] != null
                                ? ImageBaseUrl + item["poster_path"]
                                : "/Assets/Images/default_series.png",

                            BackdropUrl = item["backdrop_path"] != null
                                ? BackdropBaseUrl + item["backdrop_path"]
                                : null,

                            Status = "İzlenecek",
                            AddedDate = DateTime.Now,
                            MyCurrentSeason = 1,
                            MyCurrentEpisode = 0
                        };

                        seriesList.Add(series);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dizi Arama Hatası: {ex.Message}");
            }

            return seriesList;
        }
        // ==========================================
        // 3. FİLM DETAY GETİR (Süre, Türler, Yönetmen ve Oyuncular)
        // ==========================================
        public async Task<Movie> GetMovieDetailsAsync(string movieId)
        {
            try
            {
                // Ana detay bilgisi
                string url = $"{BaseUrl}/movie/{movieId}?api_key={ApiKey}&language=tr-TR";
                var json = await GetJsonAsync(url);

                if (json == null) return null;

                var movie = new Movie
                {
                    Id = json["id"]?.ToString(),
                    Title = json["title"]?.ToString(),
                    Synopsis = json["overview"]?.ToString(),
                    OriginalLanguage = json["original_language"]?.ToString(),
                    GlobalRating = json["vote_average"] != null ? (double)json["vote_average"] : 0,

                    // Poster ve Backdrop
                    PosterUrl = json["poster_path"] != null ? ImageBaseUrl + json["poster_path"] : "/Assets/Images/default_movie.png",
                    BackdropUrl = json["backdrop_path"] != null ? BackdropBaseUrl + json["backdrop_path"] : null,

                    // SÜRE (Search'te gelmez, burada gelir)
                    DurationMinutes = json["runtime"] != null ? (int)json["runtime"] : 0,

                    // Durum ve Varsayılanlar
                    UserStatus = WatchStatus.PlanToWatch,
                    Genres = new List<string>(),
                    Cast = new List<CastMember>()
                };

                // Tarih (Yıl)
                if (json["release_date"] != null && DateTime.TryParse(json["release_date"].ToString(), out DateTime date))
                {
                    movie.Year = date.Year;
                }

                // Türleri (Genres) Çekme
                if (json["genres"] != null)
                {
                    foreach (var genre in json["genres"])
                    {
                        movie.Genres.Add(genre["name"]?.ToString());
                    }
                }

                // Credits API - Yönetmen ve Oyuncular
                try
                {
                    string creditsUrl = $"{BaseUrl}/movie/{movieId}/credits?api_key={ApiKey}&language=tr-TR";
                    var creditsJson = await GetJsonAsync(creditsUrl);

                    if (creditsJson != null)
                    {
                        // Yönetmen (crew'dan "Director" job'ı olanı bul)
                        if (creditsJson["crew"] != null)
                        {
                            foreach (var crew in creditsJson["crew"])
                            {
                                if (crew["job"]?.ToString() == "Director")
                                {
                                    movie.Director = crew["name"]?.ToString();
                                    break;
                                }
                            }
                        }

                        // Oyuncular (ilk 10 oyuncu)
                        if (creditsJson["cast"] != null)
                        {
                            int count = 0;
                            foreach (var cast in creditsJson["cast"])
                            {
                                if (count >= 10) break;
                                
                                movie.Cast.Add(new CastMember
                                {
                                    Name = cast["name"]?.ToString(),
                                    Character = cast["character"]?.ToString(),
                                    ImageUrl = cast["profile_path"] != null 
                                        ? ImageBaseUrl + cast["profile_path"] 
                                        : null
                                });
                                count++;
                            }
                        }
                    }
                }
                catch (Exception creditsEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Credits Çekme Hatası: {creditsEx.Message}");
                }

                // Release Dates API - Yaş Sınırı (TR veya US)
                try
                {
                    string releaseDatesUrl = $"{BaseUrl}/movie/{movieId}/release_dates?api_key={ApiKey}";
                    var releaseDatesJson = await GetJsonAsync(releaseDatesUrl);

                    if (releaseDatesJson != null && releaseDatesJson["results"] != null)
                    {
                        // Önce TR, sonra US dene
                        string[] countries = { "TR", "US" };
                        foreach (var countryCode in countries)
                        {
                            foreach (var result in releaseDatesJson["results"])
                            {
                                if (result["iso_3166_1"]?.ToString() == countryCode)
                                {
                                    var releases = result["release_dates"];
                                    if (releases != null)
                                    {
                                        foreach (var release in releases)
                                        {
                                            var cert = release["certification"]?.ToString();
                                            if (!string.IsNullOrEmpty(cert))
                                            {
                                                movie.MpaaRating = cert;
                                                break;
                                            }
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(movie.MpaaRating)) break;
                                }
                            }
                            if (!string.IsNullOrEmpty(movie.MpaaRating)) break;
                        }
                    }
                }
                catch (Exception ratingEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Rating Çekme Hatası: {ratingEx.Message}");
                }

                return movie;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Detay Çekme Hatası: {ex.Message}");
                return null;
            }
        }

        // ==========================================
        // 4. DİZİ DETAY GETİR (Sezon Bilgileri, Oyuncular)
        // ==========================================
        public async Task<Series> GetSeriesDetailsAsync(int tmdbId)
        {
            try
            {
                // Ana dizi detayları
                string url = $"{BaseUrl}/tv/{tmdbId}?api_key={ApiKey}&language=tr-TR&append_to_response=credits";
                var json = await GetJsonAsync(url);

                if (json == null || json["id"] == null) return null;

                var series = new Series
                {
                    TmdbId = (int)json["id"],
                    Title = json["name"]?.ToString(),
                    OriginalName = json["original_name"]?.ToString(),
                    Overview = json["overview"]?.ToString(),
                    FirstAirDate = json["first_air_date"]?.ToString(),
                    TmdbRating = json["vote_average"] != null ? (double)json["vote_average"] : 0,
                    TotalSeasons = json["number_of_seasons"] != null ? (int)json["number_of_seasons"] : 0,
                    TotalEpisodes = json["number_of_episodes"] != null ? (int)json["number_of_episodes"] : 0,
                    AvgEpisodeRuntime = (json["episode_run_time"] != null && json["episode_run_time"].HasValues) 
                        ? (int)json["episode_run_time"][0] : 45,
                    
                    CoverImageUrl = json["poster_path"] != null
                        ? ImageBaseUrl + json["poster_path"]
                        : "/Assets/Images/default_series.png",

                    BackdropUrl = json["backdrop_path"] != null
                        ? BackdropBaseUrl + json["backdrop_path"]
                        : null,
                        
                    Network = json["networks"]?[0]?["name"]?.ToString() ?? "",
                    OriginalLanguage = json["original_language"]?.ToString()?.ToUpper() ?? "",
                    
                    Status = "İzlenecek",
                    AddedDate = DateTime.Now,
                    MyCurrentSeason = 1,
                    MyCurrentEpisode = 0
                };
                
                // Türler
                if (json["genres"] != null)
                {
                    var genreList = new List<string>();
                    foreach (var g in json["genres"])
                    {
                        genreList.Add(g["name"]?.ToString());
                    }
                    series.Genres = string.Join(", ", genreList);
                }
                
                // Cast
                if (json["credits"]?["cast"] != null)
                {
                    var castList = new List<CastMember>();
                    int count = 0;
                    foreach (var c in json["credits"]["cast"])
                    {
                        if (count >= 10) break;
                        castList.Add(new CastMember
                        {
                            Name = c["name"]?.ToString(),
                            Character = c["character"]?.ToString(),
                            ImageUrl = c["profile_path"] != null 
                                ? ImageBaseUrl + c["profile_path"]
                                : "/Assets/Images/default_actor.png"
                        });
                        count++;
                    }
                    series.Cast = castList;
                }

                return series;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dizi Detay Hatası: {ex.Message}");
                return null;
            }
        }

        // ==========================================
        // 5. SEZON DETAY GETİR (Bölüm Listesi)
        // ==========================================
        public async Task<List<Episode>> GetSeasonDetailsAsync(int tmdbId, int seasonNumber)
        {
            var episodes = new List<Episode>();
            
            try
            {
                string url = $"{BaseUrl}/tv/{tmdbId}/season/{seasonNumber}?api_key={ApiKey}&language=tr-TR";
                var json = await GetJsonAsync(url);

                if (json["episodes"] != null)
                {
                    foreach (var ep in json["episodes"])
                    {
                        episodes.Add(new Episode
                        {
                            SeasonNumber = seasonNumber,
                            EpisodeNumber = ep["episode_number"] != null ? (int)ep["episode_number"] : 0,
                            Name = ep["name"]?.ToString(),
                            Overview = ep["overview"]?.ToString(),
                            AirDate = ep["air_date"]?.ToString(),
                            Runtime = ep["runtime"] != null ? (int)ep["runtime"] : 0,
                            StillPath = ep["still_path"] != null 
                                ? ImageBaseUrl + ep["still_path"]
                                : null
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sezon Detay Hatası: {ex.Message}");
            }

            return episodes;
        }

        // ==========================================
        // YARDIMCI METOT
        // ==========================================
        private async Task<JObject> GetJsonAsync(string url)
        {
            try
            {
                var response = await _httpClient.GetStringAsync(url);
                return JObject.Parse(response);
            }
            catch
            {
                return new JObject();
            }
        }
    }
}