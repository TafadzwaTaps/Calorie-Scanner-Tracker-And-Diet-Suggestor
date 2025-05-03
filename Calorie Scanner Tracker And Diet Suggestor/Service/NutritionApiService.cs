using System.Net.Http.Headers;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Service
{
    public class NutritionApiService
    {
        private readonly HttpClient _httpClient;

        public NutritionApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> AnalyzeImageAsync(IFormFile file)
        {
            using var content = new MultipartFormDataContent();
            using var stream = file.OpenReadStream();
            using var fileContent = new StreamContent(stream);

            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.FileName);

            var response = await _httpClient.PostAsync("http://127.0.0.1:8000/analyze", content);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
    }
}
