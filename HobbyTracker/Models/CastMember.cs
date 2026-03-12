namespace HobbyTracker.Models
{
    /// <summary>
    /// Film veya dizi oyuncusu modeli.
    /// </summary>
    public class CastMember
    {
        /// <summary>
        /// Oyuncu adı.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Oyuncunun canlandırdığı karakter adı.
        /// </summary>
        public string Character { get; set; } = string.Empty;
        
        /// <summary>
        /// Oyuncu fotoğrafı URL'i.
        /// </summary>
        public string ImageUrl { get; set; } = string.Empty;
    }
}