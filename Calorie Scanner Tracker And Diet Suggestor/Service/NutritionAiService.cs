using Calorie_Scanner_Tracker_And_Diet_Suggestor.Models.AI;
using Newtonsoft.Json;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Service
{
    public class NutritionAiService
    {
        private readonly HttpClient _httpClient;

        public NutritionAiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<AiResponse> AnalyzeAsync(string imagePath)
        {
            using var form = new MultipartFormDataContent();

            var stream = File.OpenRead(imagePath);

            form.Add(new StreamContent(stream), "file", Path.GetFileName(imagePath));

            var response = await _httpClient.PostAsync("https://nutritionalfoodapi-c0ye.onrender.com/analyze", form);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<AiResponse>(json);
        }
    }
}