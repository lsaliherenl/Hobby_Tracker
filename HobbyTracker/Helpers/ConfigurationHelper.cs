using System;
using System.IO;
using System.Text.Json;

namespace HobbyTracker.Helpers
{
    public static class ConfigurationHelper
    {
        private static readonly string _secretsFilePath;
        private static Secrets _secrets;

        static ConfigurationHelper()
        {
            try
            {
                // AppDomain'in ana dizinini alıyoruz
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                
                // Proje kök dizinindeki secrets.json dosyasına ulaşmak için (bin/Debug/netX.X-windows vs. klasörlerini geri gidiyoruz)
                // Bu yol proje yapınıza ve çalışma klasörünüze bağlı olarak değişebilir.
                // Şimdilik en güvenli yöntem, çalıştırılabilir dosyanın olduğu yere göre projenin root'unu bulmak.
                
                // Genellikle WPF projelerinde debug/release klasöründen çıkmak için 3-4 üst klasöre gitmek gerekir.
                // O yüzden ilk olarak BaseDirectory'e bakalım, yoksa üstlere çıkalım.
                
                string directPath = Path.Combine(baseDir, "secrets.json");
                
                // Projenin kök dizinindeysek buradan bulacak. 
                // Build anında secrets.json çıktı klasörüne (Copy to Output Directory = PreserveNewest/Always)
                // kopyalanırsa çalışma anında hep çalışacaktır. 
                // Bu en tavsiye edilen yaklaşımdır.
                
                _secretsFilePath = directPath;

                LoadSecrets();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ConfigurationHelper Başlatılırken Hata: {ex.Message}");
            }
        }

        private static void LoadSecrets()
        {
            if (File.Exists(_secretsFilePath))
            {
                try
                {
                    string jsonString = File.ReadAllText(_secretsFilePath);
                    _secrets = JsonSerializer.Deserialize<Secrets>(jsonString);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"secrets.json okunamadı veya deserialization hatası: {ex.Message}");
                }
            }
            else
            {
                // Çıktı klasöründe yoksa projenin kaynağına bakmaya çalış (Geliştirme sırası için fallback)
                string projectRootFallback = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "secrets.json");
                if (File.Exists(projectRootFallback))
                {
                     try
                    {
                        string jsonString = File.ReadAllText(projectRootFallback);
                        _secrets = JsonSerializer.Deserialize<Secrets>(jsonString);
                        System.Diagnostics.Debug.WriteLine("Fallback ile secrets.json bulundu ve okundu.");
                    }
                    catch (Exception ex)
                    {
                         System.Diagnostics.Debug.WriteLine($"Fallback secrets.json okunamadı: {ex.Message}");
                    }
                }
                else
                {
                     System.Diagnostics.Debug.WriteLine($"UYARI: {_secretsFilePath} veya fallback yolunda secrets.json bulunamadı. API Key'ler boş dönecek.");
                }
            }
        }

        public static string GetTmdbApiKey() => _secrets?.TmdbApiKey ?? string.Empty;
        public static string GetFirebaseApiKey() => _secrets?.FirebaseApiKey ?? string.Empty;
        public static string GetRawgApiKey() => _secrets?.RawgApiKey ?? string.Empty;

        private class Secrets
        {
            public string TmdbApiKey { get; set; }
            public string FirebaseApiKey { get; set; }
            public string RawgApiKey { get; set; }
        }
    }
}
