using Firebase.Auth;
using Firebase.Auth.Providers;
using Firebase.Database;
using Firebase.Database.Query;
using HobbyTracker.Interfaces;
using HobbyTracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HobbyTracker.Services
{
    /// <summary>
    /// Firebase Authentication ve Realtime Database işlemlerini yöneten servis sınıfı.
    /// </summary>
    public class SFirebase : IHobbyService
    {
        private static string ApiKey => HobbyTracker.Helpers.ConfigurationHelper.GetFirebaseApiKey();
        private const string BaseUrl = "https://hobbytrackerapp-9abab-default-rtdb.europe-west1.firebasedatabase.app/";

        private readonly FirebaseAuthClient _authClient;
        private readonly FirebaseClient _firebaseClient;

        /// <summary>
        /// Firebase servisini başlatır. Authentication ve Database istemcilerini yapılandırır.
        /// </summary>
        public SFirebase()
        {
            var config = new FirebaseAuthConfig
            {
                ApiKey = ApiKey,
                AuthDomain = "hobbytrackerapp-9abab.firebaseapp.com",
                Providers = new FirebaseAuthProvider[]
                {
                    new EmailProvider()
                }
            };

            _authClient = new FirebaseAuthClient(config);
            
            // FirebaseClient'ı auth token fabrikası ile yapılandır
            // Bu sayede her istekte güncel token kullanılır
            var options = new FirebaseOptions
            {
                AuthTokenAsyncFactory = () => Task.FromResult(UserSession.UserToken)
            };
            
            _firebaseClient = new FirebaseClient(BaseUrl, options);
        }

        /// <summary>
        /// Yeni kullanıcı kaydı oluşturur.
        /// </summary>
        /// <param name="name">Kullanıcı adı.</param>
        /// <param name="email">E-posta adresi.</param>
        /// <param name="password">Şifre.</param>
        /// <returns>Başarılı ise "OK", hata durumunda hata mesajı.</returns>
        public async Task<string> RegisterUserAsync(string name, string email, string password)
        {
            try
            {
                var userCredential = await _authClient.CreateUserWithEmailAndPasswordAsync(email, password, name);

                UserSession.CurrentUserId = userCredential.User.Uid;
                UserSession.CurrentUserEmail = userCredential.User.Info.Email;
                UserSession.CurrentUserName = userCredential.User.Info.DisplayName;
                
                // Auth token'ı kaydet - Database erişimi için kritik!
                // Not: UserCredential.User.Credential.IdToken genellikle null döner, 
                // bu yüzden taze bir token alıyoruz.
                UserSession.UserToken = await userCredential.User.GetIdTokenAsync();

                await _firebaseClient
                    .Child("Users")
                    .Child(UserSession.CurrentUserId)
                    .Child("Profile")
                    .PutAsync(new
                    {
                        Username = name,
                        Email = email,
                        CreatedAt = DateTime.Now
                    });

                return "OK";
            }
            catch (FirebaseAuthException ex)
            {
                if (ex.Reason == AuthErrorReason.EmailExists) return "Bu e-posta zaten kullanılıyor.";
                if (ex.Reason == AuthErrorReason.WeakPassword) return "Şifre çok zayıf.";
                return $"Kayıt hatası: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Bilinmeyen hata: {ex.Message}";
            }
        }

        /// <summary>
        /// Kullanıcı girişi yapar.
        /// </summary>
        /// <param name="email">E-posta adresi.</param>
        /// <param name="password">Şifre.</param>
        /// <returns>Başarılı ise "OK", hata durumunda hata mesajı.</returns>
        public async Task<string> LoginUserAsync(string email, string password)
        {
            try
            {
                var userCredential = await _authClient.SignInWithEmailAndPasswordAsync(email, password);

                UserSession.CurrentUserId = userCredential.User.Uid;
                UserSession.CurrentUserEmail = userCredential.User.Info.Email;
                UserSession.CurrentUserName = userCredential.User.Info.DisplayName;
                
                // Auth token'ı kaydet - Database erişimi için kritik!
                UserSession.UserToken = await userCredential.User.GetIdTokenAsync();

                return "OK";
            }
            catch (FirebaseAuthException ex)
            {
                if (ex.Reason == AuthErrorReason.WrongPassword) return "Hatalı şifre.";
                if (ex.Reason == AuthErrorReason.UnknownEmailAddress) return "Kullanıcı bulunamadı.";
                return $"Giriş hatası: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Bilinmeyen hata: {ex.Message}";
            }
        }

        /// <summary>
        /// Yeni bir oyun ekler veya mevcut oyunu günceller.
        /// </summary>
        /// <param name="game">Eklenecek veya güncellenecek oyun.</param>
        /// <returns>Başarılı ise "OK", hata durumunda hata mesajı.</returns>
        public async Task<string> AddGameAsync(Game game)
        {
            try
            {
                if (string.IsNullOrEmpty(UserSession.CurrentUserId))
                {
                    return "Kullanıcı giriş yapmamış.";
                }

                if (string.IsNullOrEmpty(game.Id))
                {
                    game.Id = Guid.NewGuid().ToString();
                }

                await _firebaseClient
                    .Child("Users")
                    .Child(UserSession.CurrentUserId)
                    .Child("Games")
                    .Child(game.Id)
                    .PutAsync(game);

                return "OK";
            }
            catch (Exception ex)
            {
                return $"Oyun kaydedilirken hata oluştu: {ex.Message}";
            }
        }

        /// <summary>
        /// Kullanıcının tüm oyunlarını getirir.
        /// </summary>
        /// <returns>Oyun listesi. Hata durumunda boş liste.</returns>
        public async Task<List<Game>> GetGamesAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(UserSession.CurrentUserId))
                {
                    return new List<Game>();
                }

                var firebaseObject = await _firebaseClient
                    .Child("Users")
                    .Child(UserSession.CurrentUserId)
                    .Child("Games")
                    .OnceAsync<Game>();

                var games = new List<Game>();
                foreach (var item in firebaseObject)
                {
                    var game = item.Object;
                    if (game != null)
                    {
                        game.Id = item.Key;
                        games.Add(game);
                    }
                }

                return games;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Oyunlar yüklenirken hata: {ex.Message}");
                return new List<Game>();
            }
        }

        /// <summary>
        /// Mevcut bir oyunu günceller.
        /// </summary>
        /// <param name="game">Güncellenecek oyun.</param>
        /// <returns>Başarılı ise "OK", hata durumunda hata mesajı.</returns>
        public async Task<string> UpdateGameAsync(Game game)
        {
            try
            {
                if (string.IsNullOrEmpty(UserSession.CurrentUserId))
                {
                    return "Kullanıcı giriş yapmamış.";
                }

                if (string.IsNullOrEmpty(game.Id))
                {
                    return "Oyun ID'si bulunamadı.";
                }

                await _firebaseClient
                    .Child("Users")
                    .Child(UserSession.CurrentUserId)
                    .Child("Games")
                    .Child(game.Id)
                    .PutAsync(game);

                return "OK";
            }
            catch (Exception ex)
            {
                return $"Oyun güncellenirken hata oluştu: {ex.Message}";
            }
        }
        
        /// <summary>
        /// Belirtilen ID'ye sahip oyunu getirir.
        /// </summary>
        /// <param name="gameId">Oyun kimliği.</param>
        /// <returns>Oyun nesnesi. Bulunamazsa null.</returns>
        public async Task<Game?> GetGameAsync(string gameId)
        {
            try
            {
                if (string.IsNullOrEmpty(UserSession.CurrentUserId) || string.IsNullOrEmpty(gameId)) return null;

                var game = await _firebaseClient
                    .Child("Users")
                    .Child(UserSession.CurrentUserId)
                    .Child("Games")
                    .Child(gameId)
                    .OnceSingleAsync<Game>();

                return game;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Belirtilen ID'ye sahip oyunu siler.
        /// </summary>
        /// <param name="gameId">Silinecek oyunun kimliği.</param>
        /// <returns>Başarılı ise "OK", hata durumunda hata mesajı.</returns>
        public async Task<string> DeleteGameAsync(string gameId)
        {
            try
            {
                if (string.IsNullOrEmpty(UserSession.CurrentUserId))
                {
                    return "Kullanıcı giriş yapmamış.";
                }

                if (string.IsNullOrEmpty(gameId))
                {
                    return "Oyun ID'si bulunamadı.";
                }

                await _firebaseClient
                    .Child("Users")
                    .Child(UserSession.CurrentUserId)
                    .Child("Games")
                    .Child(gameId)
                    .DeleteAsync();

                return "OK";
            }
            catch (Exception ex)
            {
                return $"Oyun silinirken hata oluştu: {ex.Message}";
            }
        }

        /// <summary>
        /// Kullanıcının tüm istatistiklerini getirir.
        /// </summary>
        /// <returns>UserStats nesnesi. Hata durumunda boş istatistik.</returns>
        public async Task<UserStats> GetUserStatsAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(UserSession.CurrentUserId))
                {
                    return new UserStats();
                }

                var stats = new UserStats();

                // Oyunları çek ve istatistikleri hesapla
                var games = await GetGamesAsync();
                stats.TotalGames = games.Count;
                stats.FinishedGames = games.Count(g => g.Status == "Tamamlandı");
                stats.TotalGameHours = games.Sum(g => g.PlayTime);

                // Kitapları çek ve istatistikleri hesapla
                var books = await GetBooksAsync();
                stats.TotalBooks = books.Count;
                stats.ReadBooks = books.Count(b => b.Status == "Tamamlandı" || b.Status == "Okundu");
                stats.TotalPagesRead = books.Sum(b => b.CurrentPage);

                // Filmleri çek ve istatistikleri hesapla
                var movies = await GetMoviesAsync();
                stats.TotalMovies = movies.Count;
                stats.WatchedMovies = movies.Count(m => m.UserStatus == WatchStatus.Completed);
                stats.TotalMovieMinutes = movies.Where(m => m.UserStatus == WatchStatus.Completed).Sum(m => m.DurationMinutes);

                // Dizileri çek ve istatistikleri hesapla
                var seriesList = await GetSeriesAsync();
                stats.TotalSeries = seriesList.Count;
                stats.TotalEpisodesWatched = seriesList.Sum(s => s.WatchedEpisodesCount);
                stats.TotalSeriesMinutes = seriesList.Sum(s => s.WatchedEpisodesCount * 45); // Varsayılan 45 dk

                // Aktivite serisini (streak) hesapla
                stats.ActivityStreak = await CalculateActivityStreakAsync(games, books, movies, seriesList);

                // Haftalık aktivite verisi
                stats.WeeklyActivity = CalculateWeeklyActivity(games, books, movies, seriesList);

                return stats;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"İstatistikler yüklenirken hata: {ex.Message}");
                return new UserStats();
            }
        }

        // --- AKTİVİTE SERİSİ HESAPLAMA (STREAK) ---
        private async Task<int> CalculateActivityStreakAsync(List<Game> games, List<Book> books, List<Movie> movies, List<Series> series)
        {
            try
            {
                // Tüm ekleme tarihlerini topla
                var allDates = new List<DateTime>();
                
                allDates.AddRange(games.Select(g => g.AddedDate.Date));
                allDates.AddRange(books.Select(b => b.AddedDate.Date));
                allDates.AddRange(movies.Select(m => m.AddedDate.Date));
                allDates.AddRange(series.Select(s => s.AddedDate.Date));

                if (!allDates.Any()) return 0;

                // Tarihleri grupla ve sırala
                var uniqueDates = allDates.Distinct().OrderByDescending(d => d).ToList();
                
                // Bugünden geriye doğru ardışık günleri say
                int streak = 0;
                var checkDate = DateTime.Today;

                foreach (var date in uniqueDates)
                {
                    if (date == checkDate)
                    {
                        streak++;
                        checkDate = checkDate.AddDays(-1);
                    }
                    else if (date < checkDate)
                    {
                        break;
                    }
                }

                return streak;
            }
            catch
            {
                return 0;
            }
        }

        // --- HAFTALIK AKTİVİTE VERİSİ (GRAFİK İÇİN) ---
        private List<DailyActivity> CalculateWeeklyActivity(List<Game> games, List<Book> books, List<Movie> movies, List<Series> series)
        {
            var result = new List<DailyActivity>();
            var today = DateTime.Today;
            var dayNames = new[] { "Paz", "Pzt", "Sal", "Çar", "Per", "Cum", "Cmt" };

            // Son 7 gün
            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var count = 0;

                count += games.Count(g => g.AddedDate.Date == date);
                count += books.Count(b => b.AddedDate.Date == date);
                count += movies.Count(m => m.AddedDate.Date == date);
                count += series.Count(s => s.AddedDate.Date == date);

                result.Add(new DailyActivity
                {
                    Date = date,
                    DayName = dayNames[(int)date.DayOfWeek],
                    ActivityCount = count
                });
            }

            return result;
        }

        // --- SON EKLENEN İÇERİKLER (Dashboard için) ---
        public async Task<List<RecentItem>> GetRecentItemsAsync(int count = 5)
        {
            try
            {
                if (string.IsNullOrEmpty(UserSession.CurrentUserId))
                    return new List<RecentItem>();

                var recentItems = new List<RecentItem>();

                // Tüm içerikleri çek
                var games = await GetGamesAsync();
                var books = await GetBooksAsync();
                var movies = await GetMoviesAsync();
                var seriesList = await GetSeriesAsync();

                // Oyunları ekle
                foreach (var game in games)
                {
                    recentItems.Add(new RecentItem
                    {
                        Id = game.Id,
                        Title = game.Title,
                        CoverUrl = game.CoverImageUrl,
                        Category = "Oyun",
                        CategoryColor = "#8b5cf6", // Mor
                        Status = game.Status,
                        AddedDate = game.AddedDate,
                        Progress = game.Status == "Tamamlandı" ? 100 : (game.Status == "Oynanıyor" ? 50 : 0)
                    });
                }

                // Kitapları ekle
                foreach (var book in books)
                {
                    var progress = book.PageCount > 0 ? (int)((double)book.CurrentPage / book.PageCount * 100) : 0;
                    recentItems.Add(new RecentItem
                    {
                        Id = book.Id,
                        Title = book.Title,
                        CoverUrl = book.CoverImageUrl,
                        Category = "Kitap",
                        CategoryColor = "#f97316", // Turuncu
                        Status = book.Status,
                        AddedDate = book.AddedDate,
                        Progress = progress
                    });
                }

                // Filmleri ekle
                foreach (var movie in movies)
                {
                    recentItems.Add(new RecentItem
                    {
                        Id = movie.Id,
                        Title = movie.Title,
                        CoverUrl = movie.PosterUrl,
                        Category = "Film",
                        CategoryColor = "#ef4444", // Kırmızı
                        Status = movie.StatusDisplay,
                        AddedDate = movie.AddedDate,
                        Progress = movie.UserStatus == WatchStatus.Completed ? 100 : 0
                    });
                }

                // Dizileri ekle
                foreach (var series in seriesList)
                {
                    var progress = series.TotalEpisodes > 0 ? (int)((double)series.WatchedEpisodesCount / series.TotalEpisodes * 100) : 0;
                    recentItems.Add(new RecentItem
                    {
                        Id = series.Id,
                        Title = series.Title,
                        CoverUrl = series.CoverImageUrl,
                        Category = "Dizi",
                        CategoryColor = "#3b82f6", // Mavi
                        Status = series.StatusDisplay,
                        AddedDate = series.AddedDate,
                        Progress = progress
                    });
                }

                // En son eklenenleri getir
                return recentItems.OrderByDescending(r => r.AddedDate).Take(count).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Son eklenenler yüklenirken hata: {ex.Message}");
                return new List<RecentItem>();
            }
        }

        // --- DEVAM EDİLECEK İÇERİKLER (Dashboard için) ---
        public async Task<List<RecentItem>> GetContinueItemsAsync(int count = 4)
        {
            try
            {
                if (string.IsNullOrEmpty(UserSession.CurrentUserId))
                    return new List<RecentItem>();

                var continueItems = new List<RecentItem>();

                // Tüm içerikleri çek
                var games = await GetGamesAsync();
                var books = await GetBooksAsync();
                var seriesList = await GetSeriesAsync();

                // Devam edilen oyunları ekle (Oynuyor durumunda olanlar)
                foreach (var game in games.Where(g => g.Status == "Oynuyor"))
                {
                    continueItems.Add(new RecentItem
                    {
                        Id = game.Id,
                        Title = game.Title,
                        CoverUrl = game.CoverImageUrl,
                        Category = "Oyun",
                        CategoryColor = "#8b5cf6",
                        Status = game.Status,
                        AddedDate = game.AddedDate,
                        Progress = 0, // Oyunlar için ilerleme barı kullanılmıyor
                        PlayTimeHours = game.PlayTime // Oynama süresi (saat)
                    });
                }

                // Devam edilen kitapları ekle (Okunuyor durumunda ve ilerleme < 100)
                foreach (var book in books.Where(b => b.Status == "Okunuyor" && b.CurrentPage < b.PageCount))
                {
                    var progress = book.PageCount > 0 ? (int)((double)book.CurrentPage / book.PageCount * 100) : 0;
                    continueItems.Add(new RecentItem
                    {
                        Id = book.Id,
                        Title = book.Title,
                        CoverUrl = book.CoverImageUrl,
                        Category = "Kitap",
                        CategoryColor = "#f97316",
                        Status = book.Status,
                        AddedDate = book.AddedDate,
                        Progress = progress
                    });
                }

                // Devam edilen dizileri ekle (İzleniyor durumunda olanlar)
                foreach (var series in seriesList.Where(s => s.UserStatus == WatchStatus.InProgress && s.WatchedEpisodesCount < s.TotalEpisodes))
                {
                    var progress = series.TotalEpisodes > 0 ? (int)((double)series.WatchedEpisodesCount / series.TotalEpisodes * 100) : 0;
                    continueItems.Add(new RecentItem
                    {
                        Id = series.Id,
                        Title = series.Title,
                        CoverUrl = series.CoverImageUrl,
                        Category = "Dizi",
                        CategoryColor = "#3b82f6",
                        Status = series.StatusDisplay,
                        AddedDate = series.AddedDate,
                        Progress = progress
                    });
                }

                // En son güncellenen ilk count kadar öğeyi getir
                return continueItems.OrderByDescending(r => r.AddedDate).Take(count).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Devam edilecekler yüklenirken hata: {ex.Message}");
                return new List<RecentItem>();
            }
        }

        // --- KİTAP EKLEME (CREATE) ---
        public async Task<string> AddBookAsync(Book book)
        {
            try
            {
                if (string.IsNullOrEmpty(UserSession.CurrentUserId)) return "Kullanıcı giriş yapmamış.";
                if (string.IsNullOrEmpty(book.Id)) book.Id = Guid.NewGuid().ToString();

                await _firebaseClient
                    .Child("Users")
                    .Child(UserSession.CurrentUserId)
                    .Child("Books")
                    .Child(book.Id)
                    .PutAsync(book);

                return "OK";
            }
            catch (Exception ex)
            {
                return $"Kitap kaydedilirken hata oluştu: {ex.Message}";
            }
        }

        // --- KİTAPLARI LİSTELEME (READ) ---
        public async Task<List<Book>> GetBooksAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(UserSession.CurrentUserId)) return new List<Book>();

                var firebaseObject = await _firebaseClient
                    .Child("Users")
                    .Child(UserSession.CurrentUserId)
                    .Child("Books")
                    .OnceAsync<Book>();

                var books = new List<Book>();
                foreach (var item in firebaseObject)
                {
                    var book = item.Object;
                    if (book != null)
                    {
                        book.Id = item.Key;
                        books.Add(book);
                    }
                }
                return books;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Kitaplar yüklenirken hata: {ex.Message}");
                return new List<Book>();
            }
        }

        // --- TEKİL KİTAP ÇEKME (GET BY ID) ---
        public async Task<Book> GetBookAsync(string bookId)
        {
            try
            {
                if (string.IsNullOrEmpty(UserSession.CurrentUserId) || string.IsNullOrEmpty(bookId)) return null;

                var book = await _firebaseClient
                    .Child("Users")
                    .Child(UserSession.CurrentUserId)
                    .Child("Books")
                    .Child(bookId)
                    .OnceSingleAsync<Book>();

                return book;
            }
            catch (Exception)
            {
                return null;
            }
        }

        // --- KİTAP GÜNCELLEME (UPDATE) ---
        public async Task<string> UpdateBookAsync(Book book)
        {
            try
            {
                if (string.IsNullOrEmpty(UserSession.CurrentUserId)) return "Kullanıcı giriş yapmamış.";
                if (string.IsNullOrEmpty(book.Id)) return "Kitap ID bulunamadı.";

                await _firebaseClient
                    .Child("Users")
                    .Child(UserSession.CurrentUserId)
                    .Child("Books")
                    .Child(book.Id)
                    .PutAsync(book);

                return "OK";
            }
            catch (Exception ex)
            {
                return $"Kitap güncellenirken hata oluştu: {ex.Message}";
            }
        }

        // --- KİTAP SİLME (DELETE) ---
        public async Task<string> DeleteBookAsync(string bookId)
        {
            try
            {
                if (string.IsNullOrEmpty(UserSession.CurrentUserId)) return "Kullanıcı giriş yapmamış.";
                if (string.IsNullOrEmpty(bookId)) return "Kitap ID bulunamadı.";

                await _firebaseClient
                    .Child("Users")
                    .Child(UserSession.CurrentUserId)
                    .Child("Books")
                    .Child(bookId)
                    .DeleteAsync();

                return "OK";
            }
            catch (Exception ex)
            {
                return $"Kitap silinirken hata oluştu: {ex.Message}";
            }
        }

        // --- KİTAP HEDEFİ (GOAL) ---
        public async Task<int> GetBookGoalAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(UserSession.CurrentUserId)) return 24; // Varsayılan

                var goal = await _firebaseClient
                    .Child("Users")
                    .Child(UserSession.CurrentUserId)
                    .Child("Goals")
                    .Child("BookGoal")
                    .OnceSingleAsync<int>();

                return goal == 0 ? 24 : goal; // 0 dönerse varsayılan 24
            }
            catch
            {
                return 24; // Hata olursa varsayılan
            }
        }

        public async Task<string> UpdateBookGoalAsync(int newGoal)
        {
            try
            {
                if (string.IsNullOrEmpty(UserSession.CurrentUserId)) return "Kullanıcı giriş yapmamış.";

                await _firebaseClient
                    .Child("Users")
                    .Child(UserSession.CurrentUserId)
                    .Child("Goals")
                    .Child("BookGoal")
                    .PutAsync(newGoal);

                return "OK";
            }
            catch (Exception ex)
            {
                return $"Hedef güncellenirken hata: {ex.Message}";
            }
        }

        // =============================================
        // FİLM METODLARI (MOVIE CRUD)
        // =============================================

        // --- FİLM EKLEME (CREATE) ---
        public async Task<string> AddMovieAsync(Movie movie)
        {
            try
            {
                if (string.IsNullOrEmpty(UserSession.CurrentUserId)) return "Kullanıcı giriş yapmamış.";
                if (string.IsNullOrEmpty(movie.Id)) movie.Id = Guid.NewGuid().ToString();

                await _firebaseClient
                    .Child("Users")
                    .Child(UserSession.CurrentUserId)
                    .Child("Movies")
                    .Child(movie.Id)
                    .PutAsync(movie);

                return "OK";
            }
            catch (Exception ex)
            {
                return $"Film kaydedilirken hata oluştu: {ex.Message}";
            }
        }

        // --- FİLMLERİ LİSTELEME (READ) ---
        public async Task<List<Movie>> GetMoviesAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(UserSession.CurrentUserId)) return new List<Movie>();

                var firebaseObject = await _firebaseClient
                    .Child("Users")
                    .Child(UserSession.CurrentUserId)
                    .Child("Movies")
                    .OnceAsync<Movie>();

                var movies = new List<Movie>();
                foreach (var item in firebaseObject)
                {
                    var movie = item.Object;
                    if (movie != null)
                    {
                        movie.Id = item.Key;
                        movies.Add(movie);
                    }
                }
                return movies;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Filmler yüklenirken hata: {ex.Message}");
                return new List<Movie>();
            }
        }

        // --- FİLM GÜNCELLEME (UPDATE) ---
        public async Task<string> UpdateMovieAsync(Movie movie)
        {
            try
            {
                if (string.IsNullOrEmpty(UserSession.CurrentUserId)) return "Kullanıcı giriş yapmamış.";
                if (string.IsNullOrEmpty(movie.Id)) return "Film ID bulunamadı.";

                await _firebaseClient
                    .Child("Users")
                    .Child(UserSession.CurrentUserId)
                    .Child("Movies")
                    .Child(movie.Id)
                    .PutAsync(movie);

                return "OK";
            }
            catch (Exception ex)
            {
                return $"Film güncellenirken hata oluştu: {ex.Message}";
            }
        }

        // --- FİLM SİLME (DELETE) ---
        public async Task<string> DeleteMovieAsync(string movieId)
        {
            try
            {
                if (string.IsNullOrEmpty(UserSession.CurrentUserId)) return "Kullanıcı giriş yapmamış.";
                if (string.IsNullOrEmpty(movieId)) return "Film ID bulunamadı.";

                await _firebaseClient
                    .Child("Users")
                    .Child(UserSession.CurrentUserId)
                    .Child("Movies")
                    .Child(movieId)
                    .DeleteAsync();

                return "OK";
            }
            catch (Exception ex)
            {
                return $"Film silinirken hata oluştu: {ex.Message}";
            }
        }

        // =============================================
        // DİZİ METODLARI (SERIES CRUD)
        // =============================================

        // --- DİZİ EKLEME (CREATE) ---
        public async Task<string> AddSeriesAsync(Series series)
        {
            try
            {
                if (string.IsNullOrEmpty(UserSession.CurrentUserId)) return "Kullanıcı giriş yapmamış.";
                if (string.IsNullOrEmpty(series.Id)) series.Id = Guid.NewGuid().ToString();

                await _firebaseClient
                    .Child("Users")
                    .Child(UserSession.CurrentUserId)
                    .Child("Series")
                    .Child(series.Id)
                    .PutAsync(series);

                return "OK";
            }
            catch (Exception ex)
            {
                return $"Dizi kaydedilirken hata oluştu: {ex.Message}";
            }
        }

        // --- DİZİLERİ LİSTELEME (READ) ---
        public async Task<List<Series>> GetSeriesAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(UserSession.CurrentUserId)) return new List<Series>();

                var firebaseObject = await _firebaseClient
                    .Child("Users")
                    .Child(UserSession.CurrentUserId)
                    .Child("Series")
                    .OnceAsync<Series>();

                var seriesList = new List<Series>();
                foreach (var item in firebaseObject)
                {
                    var series = item.Object;
                    if (series != null)
                    {
                        series.Id = item.Key;
                        seriesList.Add(series);
                    }
                }
                return seriesList;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Diziler yüklenirken hata: {ex.Message}");
                return new List<Series>();
            }
        }

        // --- DİZİ GÜNCELLEME (UPDATE) ---
        public async Task<string> UpdateSeriesAsync(Series series)
        {
            try
            {
                if (string.IsNullOrEmpty(UserSession.CurrentUserId)) return "Kullanıcı giriş yapmamış.";
                if (string.IsNullOrEmpty(series.Id)) return "Dizi ID bulunamadı.";

                await _firebaseClient
                    .Child("Users")
                    .Child(UserSession.CurrentUserId)
                    .Child("Series")
                    .Child(series.Id)
                    .PutAsync(series);

                return "OK";
            }
            catch (Exception ex)
            {
                return $"Dizi güncellenirken hata oluştu: {ex.Message}";
            }
        }

        // --- DİZİ SİLME (DELETE) ---
        public async Task<string> DeleteSeriesAsync(string seriesId)
        {
            try
            {
                if (string.IsNullOrEmpty(UserSession.CurrentUserId)) return "Kullanıcı giriş yapmamış.";
                if (string.IsNullOrEmpty(seriesId)) return "Dizi ID bulunamadı.";

                await _firebaseClient
                    .Child("Users")
                    .Child(UserSession.CurrentUserId)
                    .Child("Series")
                    .Child(seriesId)
                    .DeleteAsync();

                return "OK";
            }
            catch (Exception ex)
            {
                return $"Dizi silinirken hata oluştu: {ex.Message}";
            }
        }

        // --- TEKİL DİZİ ÇEKME (GET BY ID) ---
        public async Task<Series> GetSeriesAsync(string seriesId)
        {
            try
            {
                if (string.IsNullOrEmpty(UserSession.CurrentUserId) || string.IsNullOrEmpty(seriesId)) return null;

                var series = await _firebaseClient
                    .Child("Users")
                    .Child(UserSession.CurrentUserId)
                    .Child("Series")
                    .Child(seriesId)
                    .OnceSingleAsync<Series>();

                return series;
            }
            catch (Exception)
            {
                return null;
            }
        }

        // --- TEKİL FİLM ÇEKME (GET BY ID) ---
        public async Task<Movie> GetMovieAsync(string movieId)
        {
            try
            {
                if (string.IsNullOrEmpty(UserSession.CurrentUserId) || string.IsNullOrEmpty(movieId)) return null;

                var movie = await _firebaseClient
                    .Child("Users")
                    .Child(UserSession.CurrentUserId)
                    .Child("Movies")
                    .Child(movieId)
                    .OnceSingleAsync<Movie>();

                return movie;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}