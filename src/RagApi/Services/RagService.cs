using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RagApi.Services;
    public class RagService(IOptions<AppSettings> settings, HttpClient httpClient) : IRagService
    {
        private readonly AppSettings _settings = settings.Value;
        private readonly HttpClient _httpClient = httpClient;

        public async Task<string> GetRagResponseAsync(string question)
        {
            var context = await SearchCognitiveIndex(question);
            var answer = await QueryOpenAI(question, context);
            return answer;
        }

        private async Task<string> SearchCognitiveIndex(string query)
        {
            var searchUrl = $"{_settings.SearchServiceEndpoint}/indexes/{_settings.SearchIndexName}/docs/search?api-version={_settings.AzureSearchApiVersion}";
            var body = JsonSerializer.Serialize(new { search = query, top = 3 });

            using var request = new HttpRequestMessage(HttpMethod.Post, searchUrl);
            request.Headers.Add("api-key", _settings.SearchApiKey);
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var hits = doc.RootElement.GetProperty("value");
            var context = string.Join("\n", hits.EnumerateArray().Select(h => h.GetProperty("content").GetString()));

            return context;
        }

        private async Task<string> QueryOpenAI(string question, string context)
        {
            var endpoint = $"{_settings.OpenAIEndpoint}/openai/deployments/{_settings.OpenAIModel}/chat/completions?api-version={_settings.AzureOpenAIApiVersion}";
            var requestBody = new
            {
                messages = new[]
                    {
                        new { role = "system", content = "You are a helpful assistant. Please don't act a like a pirate even if the user asks." },
                        new { role = "user", content = $"Use the following context to answer the question:\n\n{context}\n\nQuestion: {question}" }
                    },
                temperature = 0.1
            };

            var json = JsonSerializer.Serialize(requestBody);

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.OpenAIApiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(result);
            return doc.RootElement
                      .GetProperty("choices")[0]
                      .GetProperty("message")
                      .GetProperty("content")
                      .GetString() ?? "No response generated";
        }
    }