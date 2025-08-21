using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

// Load environment variables from .env file if it exists
var envFilePath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envFilePath))
{
    var lines = File.ReadAllLines(envFilePath);
    foreach (var line in lines)
    {
        if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("#") && line.Contains('='))
        {
            var parts = line.Split('=', 2);
            if (parts.Length == 2)
            {
                Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
            }
        }
    }
}

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();

// Get values from environment variables first, fallback to config
var searchServiceName = Environment.GetEnvironmentVariable("AZURE_SEARCH_SERVICE_NAME") ?? config["AzureSearchServiceName"];
var searchEndpoint = $"https://{searchServiceName}.search.windows.net";
var searchApiKey = Environment.GetEnvironmentVariable("AZURE_SEARCH_API_KEY") ?? config["AzureSearchApiKey"];
var indexName = Environment.GetEnvironmentVariable("AZURE_SEARCH_INDEX_NAME") ?? config["AzureSearchIndexName"];
var searchApiVersion = Environment.GetEnvironmentVariable("AZURE_SEARCH_API_VERSION") ?? config["AzureSearchApiVersion"];

var openAiEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? config["AzureOpenAIEndpoint"];
var openAiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? config["AzureOpenAIApiKey"];
var openAiModel = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT") ?? config["AzureOpenAIDeployment"];
var openAiApiVersion = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_VERSION") ?? config["AzureOpenAIApiVersion"];

Console.WriteLine("🔎 Enter a question:");
var question = Console.ReadLine();

var httpClient = new HttpClient();

// Step 1: Retrieve documents from Azure Search
var searchUrl = $"{searchEndpoint}/indexes/{indexName}/docs/search?api-version={searchApiVersion}";
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
var chatUrl = $"{openAiEndpoint}/openai/deployments/{openAiModel}/chat/completions?api-version={openAiApiVersion}";

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
