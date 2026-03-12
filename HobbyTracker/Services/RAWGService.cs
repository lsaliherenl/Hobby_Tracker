using HobbyTracker.Models;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

namespace HobbyTracker.Services
{
    public class RAWGService
    {
        
        private static string ApiKey => HobbyTracker.Helpers.ConfigurationHelper.GetRawgApiKey(); 
        private const string BaseUrl = "https://api.rawg.io/api";

        private readonly RestClient _client;

        public RAWGService()
        {
            _client = new RestClient(BaseUrl);
        }

        // RAWG API'den oyun arama
        public async Task<List<RAWGGameResult>> SearchGamesAsync(string query)
        {
            try
            {
                var request = new RestRequest("/games", Method.Get);
                request.AddParameter("key", ApiKey);
                request.AddParameter("search", query);
                request.AddParameter("page_size", 20);

                var response = await _client.ExecuteAsync(request);

                if (response.IsSuccessful && !string.IsNullOrEmpty(response.Content))
                {
                    var jsonDoc = JsonDocument.Parse(response.Content);
                    var results = new List<RAWGGameResult>();
                    
                    if (jsonDoc.RootElement.TryGetProperty("results", out var resultsElement))
                    {
                        foreach (var item in resultsElement.EnumerateArray())
                        {
                            var game = new RAWGGameResult
                            {
                                Id = item.TryGetProperty("id", out var id) ? id.GetInt32() : 0,
                                Name = item.TryGetProperty("name", out var name) ? name.GetString() : "",
                                BackgroundImage = item.TryGetProperty("background_image", out var img) ? img.GetString() : "",
                                Released = item.TryGetProperty("released", out var released) ? released.GetString() : "",
                                Rating = item.TryGetProperty("rating", out var rating) ? rating.GetDouble() : 0
                            };
                            
                            if (item.TryGetProperty("developers", out var devs))
                            {
                                game.Developers = new List<RAWGDeveloper>();
                                foreach (var dev in devs.EnumerateArray())
                                {
                                    if (dev.TryGetProperty("name", out var devName))
                                    {
                                        game.Developers.Add(new RAWGDeveloper { Name = devName.GetString() });
                                    }
                                }
                            }
                            
                            if (item.TryGetProperty("genres", out var resultGenres))
                            {
                                game.Genres = new List<RAWGGenre>();
                                foreach (var g in resultGenres.EnumerateArray())
                                {
                                    if (g.TryGetProperty("name", out var gName))
                                    {
                                        game.Genres.Add(new RAWGGenre { Name = gName.GetString() });
                                    }
                                }
                            }
                            
                            results.Add(game);
                        }
                    }
                    
                    return results;
                }

                return new List<RAWGGameResult>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RAWG API arama hatası: {ex.Message}");
                return new List<RAWGGameResult>();
            }
        }

        // RAWG API'den oyun detayları çekme
        public async Task<RAWGGameDetails> GetGameDetailsAsync(int rawgId)
        {
            try
            {
                var request = new RestRequest($"/games/{rawgId}", Method.Get);
                request.AddParameter("key", ApiKey);

                var response = await _client.ExecuteAsync(request);

                if (response.IsSuccessful && !string.IsNullOrEmpty(response.Content))
                {
                    var jsonDoc = JsonDocument.Parse(response.Content);
                    var root = jsonDoc.RootElement;
                    
                    var details = new RAWGGameDetails
                    {
                        Id = root.TryGetProperty("id", out var id) ? id.GetInt32() : 0,
                        Name = root.TryGetProperty("name", out var name) ? name.GetString() : "",
                        Description = root.TryGetProperty("description", out var desc) ? desc.GetString() : "",
                        DescriptionRaw = root.TryGetProperty("description_raw", out var descRaw) ? descRaw.GetString() : "",
                        BackgroundImage = root.TryGetProperty("background_image", out var img) ? img.GetString() : "",
                        Released = root.TryGetProperty("released", out var released) ? released.GetString() : "",
                        Metacritic = root.TryGetProperty("metacritic", out var meta) ? meta.GetDouble() : null
                    };
                    
                    if (root.TryGetProperty("developers", out var devs))
                    {
                        details.Developers = new List<RAWGDeveloper>();
                        foreach (var dev in devs.EnumerateArray())
                        {
                            if (dev.TryGetProperty("name", out var devName))
                            {
                                details.Developers.Add(new RAWGDeveloper { Name = devName.GetString() });
                            }
                        }
                    }

                    if (root.TryGetProperty("platforms", out var platforms))
                    {
                        details.Platforms = new List<RAWGPlatformWrapper>();
                        foreach (var p in platforms.EnumerateArray())
                        {
                            if (p.TryGetProperty("platform", out var platformObj) && 
                                platformObj.TryGetProperty("name", out var pName))
                            {
                                details.Platforms.Add(new RAWGPlatformWrapper 
                                { 
                                    Platform = new RAWGPlatform { Name = pName.GetString() } 
                                });
                            }
                        }
                    }

                    // Genres
                    if (root.TryGetProperty("genres", out var genres))
                    {
                        details.Genres = new List<RAWGGenre>();
                        foreach (var g in genres.EnumerateArray())
                        {
                            if (g.TryGetProperty("name", out var gName))
                            {
                                details.Genres.Add(new RAWGGenre { Name = gName.GetString() });
                            }
                        }
                    }
                    
                    return details;
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RAWG API detay hatası: {ex.Message}");
                return null;
            }
        }

        // RAWG Game Result'ı Game modeline çevir
        public Game ConvertToGame(RAWGGameDetails details)
        {
            if (details == null) return null;

            return new Game
            {
                RawgId = details.Id,
                Title = details.Name,
                Developer = details.Developers?.FirstOrDefault()?.Name ?? "Bilinmiyor",
                ReleaseDate = details.Released ?? "",
                MetacriticScore = details.Metacritic ?? 0,
                Description = details.DescriptionRaw ?? details.Description ?? "",
                CoverImageUrl = details.BackgroundImage ?? "/ZImages/sample_cover.jpg",
                Genres = details.Genres != null && details.Genres.Count > 0 
                    ? string.Join(", ", details.Genres.Select(g => g.Name)) 
                    : ""
            };
        }
    }

    // RAWG API Response Modelleri
    public class RAWGGameResult
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string BackgroundImage { get; set; }
        public string Released { get; set; }
        public double Rating { get; set; }
        
        // 5 üzerinden gelen puanı 10 üzerinden göster
        public double Rating10 => Rating * 2;
        

        public List<RAWGDeveloper> Developers { get; set; }
        public List<RAWGGenre> Genres { get; set; }
    }

    public class RAWGGameDetails
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string DescriptionRaw { get; set; }
        public string BackgroundImage { get; set; }
        public string Released { get; set; }
        public double? Metacritic { get; set; }
        public List<RAWGDeveloper> Developers { get; set; }
        public List<RAWGGenre> Genres { get; set; }
        public List<RAWGPlatformWrapper> Platforms { get; set; }
    }

    public class RAWGPlatformWrapper // Maps the nested structure
    {
        public RAWGPlatform Platform { get; set; }
    }

    public class RAWGPlatform
    {
        public string Name { get; set; }
    }

    public class RAWGGenre
    {
        public string Name { get; set; }
    }

    public class RAWGDeveloper
    {
        public string Name { get; set; }
    }
}

