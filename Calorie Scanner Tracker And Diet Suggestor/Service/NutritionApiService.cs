using System.Net.Http.Headers;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Service
{
    public class NutritionApiService
    {
       


        private readonly HttpClient _httpClient;

        public NutritionApiService(HttpClient httpClient)
        {
            // FIRST: Assign the passed HttpClient to the private field
            _httpClient = httpClient;
            // SECOND: Now that _httpClient is initialized, you can set its BaseAddress
            _httpClient.BaseAddress = new Uri("http://127.0.0.1:9000/"); // Set base address
                                                                         // Note: The /analyze path should be used in PostAsync, not the base address
        }

        // Changed to accept byte[] and a fileName
        public async Task<string> AnalyzeImageAsync(byte[] imageBytes, string fileName)
        {
            using var content = new MultipartFormDataContent();
            // Using a MemoryStream to create a StreamContent from byte[]
            using var stream = new MemoryStream(imageBytes);
            using var fileContent = new StreamContent(stream);

            // Dynamically set content type based on common image types, or default to jpeg
            string contentType = GetContentType(fileName);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            // Add the file content with the correct name "file" expected by FastAPI
            content.Add(fileContent, "file", fileName);

            // Post to the /analyze endpoint.
            // Since BaseAddress is now "http://127.0.0.1:8000/", we just use "analyze" here.
            var response = await _httpClient.PostAsync("analyze", content);

            // Do not use EnsureSuccessStatusCode() here directly, as we want to read
            // custom error messages from the FastAPI response body even on 4xx/5xx statuses.
            // response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        private string GetContentType(string fileName)
        {
            // Simple heuristic to determine content type, improve as needed
            if (fileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                return "image/jpeg";
            if (fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                return "image/png";
            // Default to JPEG if unknown
            return "image/jpeg";
        }
    }
}