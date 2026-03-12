using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using HobbyTracker.Models;
using Newtonsoft.Json.Linq; // JSON işlemleri için

namespace HobbyTracker.Services
{
    public class GoogleBooksService
    {
        private readonly HttpClient _httpClient;

        public GoogleBooksService()
        {
            _httpClient = new HttpClient();
        }

        // Kitap Arama Fonksiyonu
        public async Task<List<Book>> SearchBooksAsync(string query)
        {
            List<Book> books = new List<Book>();

            // Boş arama yapılmasını engelle
            if (string.IsNullOrWhiteSpace(query)) return books;

            try
            {
                // 1. Google API'ye istek URL'sini hazırla
                // 'maxResults=20' ile en fazla 20 sonuç istiyoruz.
                string url = $"https://www.googleapis.com/books/v1/volumes?q={query}&maxResults=20";
                
                // İsteği gönder
                var response = await _httpClient.GetStringAsync(url);

                // 2. Gelen JSON verisini parçala (Parse)
                JObject json = JObject.Parse(response);
                
                // Eğer sonuç yoksa boş liste dön
                if (json["items"] == null) return books;

                // 3. Her bir kitap sonucunu bizim 'Book' modelimize çevir
                foreach (var item in json["items"])
                {
                    var volumeInfo = item["volumeInfo"];
                    
                    Book book = new Book();
                    
                    // --- TEMEL BİLGİLER ---
                    book.GoogleBooksId = item["id"]?.ToString();
                    book.Title = volumeInfo["title"]?.ToString();
                    book.Description = volumeInfo["description"]?.ToString() ?? "Açıklama bulunamadı.";
                    book.Publisher = volumeInfo["publisher"]?.ToString() ?? "Bilinmeyen Yayınevi";
                    book.PublishedDate = volumeInfo["publishedDate"]?.ToString() ?? "";
                    
                    // Sayfa Sayısı (Null gelirse 0 yap)
                    book.PageCount = (int?)volumeInfo["pageCount"] ?? 0;

                    // --- YAZARLAR ---
                    // Google yazarları dizi ["Yazar1", "Yazar2"] olarak verir. Biz string yapıyoruz.
                    if (volumeInfo["authors"] != null)
                    {
                        book.Authors = string.Join(", ", volumeInfo["authors"].ToObject<string[]>());
                    }
                    else
                    {
                        book.Authors = "Bilinmeyen Yazar";
                    }

                    // --- ISBN (BARKOD) BULMA ---
                    // ISBN_13 önceliklidir, yoksa ISBN_10 veya diğerini alırız.
                    if (volumeInfo["industryIdentifiers"] != null)
                    {
                        foreach (var id in volumeInfo["industryIdentifiers"])
                        {
                            var type = id["type"]?.ToString();
                            var identifier = id["identifier"]?.ToString();

                            if (type == "ISBN_13")
                            {
                                book.ISBN = identifier;
                                break; // En iyisini bulduk, döngüden çık
                            }
                            else if (type == "ISBN_10")
                            {
                                book.ISBN = identifier;
                            }
                        }
                    }

                    // --- KAPAK RESMİ ---
                    // Google resimleri bazen 'http' olarak verir, güvenli olması için 'https' yapıyoruz.
                    if (volumeInfo["imageLinks"] != null && volumeInfo["imageLinks"]["thumbnail"] != null)
                    {
                        string imgUrl = volumeInfo["imageLinks"]["thumbnail"].ToString();
                        book.CoverImageUrl = imgUrl.Replace("http://", "https://");
                    }
                    else
                    {
                        // Resim yoksa varsayılan bir gri kutu veya placeholder
                        book.CoverImageUrl = "/ZImages/default_book.png"; 
                    }

                    // Varsayılan Kullanıcı Verileri
                    book.Status = "İstek Listesi"; // İlk eklendiğinde varsayılan durum
                    book.AddedDate = DateTime.Now;

                    // Listeye ekle
                    books.Add(book);
                }
            }
            catch (Exception ex)
            {
                // İnternet hatası vb. durumlar için loglama yapılabilir
                System.Diagnostics.Debug.WriteLine("API Hatası: " + ex.Message);
            }

            return books;
        }
    }
}