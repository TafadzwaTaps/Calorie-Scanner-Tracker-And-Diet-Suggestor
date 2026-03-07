using Calorie_Scanner_Tracker_And_Diet_Suggestor.DTOs;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Service
{
    public class NutritionApiService
    {
        private readonly HttpClient _httpClient;

        public NutritionApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromMinutes(10);
            _httpClient.BaseAddress = new Uri("https://rathious-nutritional-food-api.hf.space/");
        }

        public async Task<FoodAnalysisResult> AnalyzeImageAsync(byte[] imageBytes, string fileName)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                using var stream = new MemoryStream(imageBytes);
                using var fileContent = new StreamContent(stream);

                fileContent.Headers.ContentType =
                    new MediaTypeHeaderValue(GetContentType(fileName));

                content.Add(fileContent, "file", fileName);

                var response = await _httpClient.PostAsync("analyze", content);

                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new FoodAnalysisResult
                    {
                        Error = $"Nutrition API error: {response.StatusCode} | {json}"
                    };
                }

                if (string.IsNullOrWhiteSpace(json))
                {
                    return new FoodAnalysisResult
                    {
                        Error = "Nutrition API returned empty response"
                    };
                }

                var apiResult = JsonSerializer.Deserialize<FoodAnalysisApiResponse>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (apiResult == null)
                {
                    return new FoodAnalysisResult
                    {
                        Error = "Invalid JSON from nutrition API"
                    };
                }

                return new FoodAnalysisResult
                {
                    Calories = apiResult.Calories,
                    Protein = apiResult.Protein,
                    Carbs = apiResult.Carbs,
                    Fats = apiResult.Fats,
                    WarningMessage = apiResult.Warning
                };
            }
            catch (Exception ex)
            {
                return new FoodAnalysisResult
                {
                    Error = $"Service exception: {ex.Message}"
                };
            }
        }

        private string GetContentType(string fileName)
        {
            if (fileName.EndsWith(".jpeg") || fileName.EndsWith(".jpg")) return "image/jpeg";
            if (fileName.EndsWith(".png")) return "image/png";
            return "image/jpeg";
        }

        private class FoodAnalysisApiResponse
        {
            public decimal Carbs { get; set; }
            public decimal Fats { get; set; }
            public decimal Protein { get; set; }
            public decimal Calories { get; set; }
            public string? Error { get; set; }
            public string? Warning { get; set; }
        }
    }
}