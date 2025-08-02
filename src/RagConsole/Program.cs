using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var searchEndpoint = config["SearchServiceEndpoint"];
var searchApiKey = config["SearchApiKey"];
var indexName = config["SearchIndexName"];

var openAiEndpoint = config["OpenAIEndpoint"];
var openAiKey = config["OpenAIApiKey"];
var openAiModel = config["OpenAIModel"];

Console.WriteLine("🔎 Enter a question:");
var question = Console.ReadLine();

var httpClient = new HttpClient();

// Step 1: Retrieve documents from Azure Search
var searchUrl = $"{searchEndpoint}/indexes/{indexName}/docs/search?api-version=2023-07-01-preview";
var searchBody = JsonSerializer.Serialize(new { search = question, top = 3 });

var searchRequest = new HttpRequestMessage(HttpMethod.Post, searchUrl);
searchRequest.Headers.Add("api-key", searchApiKey);
searchRequest.Content = new StringContent(searchBody, Encoding.UTF8, "application/json");

var searchResponse = await httpClient.SendAsync(searchRequest);
var searchJson = await searchResponse.Content.ReadAsStringAsync();

using var searchDoc = JsonDocument.Parse(searchJson);
var hits = searchDoc.RootElement.GetProperty("value");
var context = string.Join("\n", hits.EnumerateArray().Select(h => h.GetProperty("content").GetString()));

Console.WriteLine("\n Retrieved Context:\n");
Console.WriteLine(context);

// Step 2: Call Azure OpenAI
var chatUrl = $"{openAiEndpoint}/openai/deployments/{openAiModel}/chat/completions?api-version=2024-05-01";

var chatRequest = new
{
    messages = new[]
    {
        new { role = "system", content = "You are a helpful assistant." },
        new { role = "user", content = $"Using the following context:\n\n{context}\n\nAnswer this: {question}" }
    },
    temperature = 0.5
};

httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", openAiKey);
var openAiContent = new StringContent(JsonSerializer.Serialize(chatRequest), Encoding.UTF8, "application/json");

var openAiResponse = await httpClient.PostAsync(chatUrl, openAiContent);
var openAiJson = await openAiResponse.Content.ReadAsStringAsync();

using var openAiDoc = JsonDocument.Parse(openAiJson);
var answer = openAiDoc.RootElement
    .GetProperty("choices")[0]
    .GetProperty("message")
    .GetProperty("content")
    .GetString();

Console.WriteLine("\n GPT-4 Answer:\n");
Console.WriteLine(answer);
