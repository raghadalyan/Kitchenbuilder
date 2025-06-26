using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DotNetEnv;

namespace Kitchenbuilder.Core
{
    public static class ConnectToAI
    {
        private static readonly HttpClient httpClient = new();
        private static string? apiKey;

        static ConnectToAI()
        {
            // Load API key from .env only once
            Env.Load(@"C:\Users\chouse\Downloads\Kitchenbuilder\Kitchenbuilder\.env");
            apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        }

        public static async Task<string> SendPromptAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return "❌ API key is missing.";

            var request = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant that helps design kitchen cabinets." },
                    new { role = "user", content = prompt }
                }
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var response = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            if (!response.IsSuccessStatusCode)
                return $"❌ API error: {response.StatusCode}";

            var responseString = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(responseString);
            return doc.RootElement
                      .GetProperty("choices")[0]
                      .GetProperty("message")
                      .GetProperty("content")
                      .GetString();
        }
    }
}
