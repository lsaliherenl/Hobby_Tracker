using HobbyTracker.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HobbyTracker.Interfaces
{
    /// <summary>
    /// Hobi verilerini yönetmek için servis arayüzü. Firebase veya başka bir backend implementasyonu için kullanılabilir.
    /// </summary>
    public interface IHobbyService
    {
        /// <summary>
        /// Yeni kullanıcı kaydı oluşturur.
        /// </summary>
        Task<string> RegisterUserAsync(string name, string email, string password);
        
        /// <summary>
        /// Kullanıcı girişi yapar.
        /// </summary>
        Task<string> LoginUserAsync(string email, string password);
        
        /// <summary>
        /// Yeni bir oyun ekler.
        /// </summary>
        Task<string> AddGameAsync(Game game);
        
        /// <summary>
        /// Kullanıcının tüm oyunlarını getirir.
        /// </summary>
        Task<List<Game>> GetGamesAsync();
        
        /// <summary>
        /// Mevcut bir oyunu günceller.
        /// </summary>
        Task<string> UpdateGameAsync(Game game);
        
        /// <summary>
        /// Bir oyunu siler.
        /// </summary>
        Task<string> DeleteGameAsync(string gameId);
        
        /// <summary>
        /// Kullanıcı istatistiklerini getirir.
        /// </summary>
        Task<UserStats> GetUserStatsAsync();
    }
}