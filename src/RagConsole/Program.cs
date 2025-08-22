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

// Validate required configuration
if (string.IsNullOrEmpty(searchServiceName))
{
    Console.WriteLine("❌ AZURE_SEARCH_SERVICE_NAME is required");
    return;
}
if (string.IsNullOrEmpty(searchApiKey))
{
    Console.WriteLine("❌ AZURE_SEARCH_API_KEY is required");
    return;
}
if (string.IsNullOrEmpty(indexName))
{
    Console.WriteLine("❌ AZURE_SEARCH_INDEX_NAME is required");
    return;
}
if (string.IsNullOrEmpty(openAiEndpoint))
{
    Console.WriteLine("❌ AZURE_OPENAI_ENDPOINT is required");
    return;
}
if (string.IsNullOrEmpty(openAiKey))
{
    Console.WriteLine("❌ AZURE_OPENAI_API_KEY is required");
    return;
}
if (string.IsNullOrEmpty(openAiModel))
{
    Console.WriteLine("❌ AZURE_OPENAI_DEPLOYMENT is required");
    return;
}

Console.WriteLine($"🔧 Using search endpoint: {searchEndpoint}");
Console.WriteLine($"🔧 Using index: {indexName}");
Console.WriteLine($"🔧 Using OpenAI endpoint: {openAiEndpoint}");
Console.WriteLine($"🔧 Using deployment: {openAiModel}");

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
if (!searchResponse.IsSuccessStatusCode)
{
    Console.WriteLine($"Search request failed: {searchResponse.StatusCode}");
    Console.WriteLine(await searchResponse.Content.ReadAsStringAsync());
    return;
}

var searchJson = await searchResponse.Content.ReadAsStringAsync();

using var searchDoc = JsonDocument.Parse(searchJson);
if (!searchDoc.RootElement.TryGetProperty("value", out var hits))
{
    Console.WriteLine("No 'value' property found in search response");
    Console.WriteLine($"Response: {searchJson}");
    return;
}

var contextParts = new List<string>();
foreach (var hit in hits.EnumerateArray())
{
    if (hit.TryGetProperty("content", out var contentProperty) && contentProperty.ValueKind == JsonValueKind.String)
    {
        var content = contentProperty.GetString();
        if (!string.IsNullOrEmpty(content))
        {
            contextParts.Add(content);
        }
    }
}

var context = string.Join("\n", contextParts);

if (string.IsNullOrEmpty(context))
{
    Console.WriteLine("No content found in search results");
    Console.WriteLine($"Search response: {searchJson}");
    return;
}

Console.WriteLine("\n Retrieved Context:\n");
Console.WriteLine(context);

// Step 2: Call Azure OpenAI
var chatUrl = $"{openAiEndpoint}openai/deployments/{openAiModel}/chat/completions?api-version={openAiApiVersion}";
Console.WriteLine($"🔧 Calling: {chatUrl}");

var chatRequest = new
{
    messages = new[]
    {
        new { role = "system", content = "You are a helpful assistant." },
        new { role = "user", content = $"Using the following context:\n\n{context}\n\nAnswer this: {question}" }
    },
    temperature = 0.5
};

// Clear any existing authorization headers
var openAiContent = new StringContent(JsonSerializer.Serialize(chatRequest), Encoding.UTF8, "application/json");

var openAiRequest = new HttpRequestMessage(HttpMethod.Post, chatUrl);
openAiRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", openAiKey);
openAiRequest.Content = openAiContent;

var openAiResponse = await httpClient.SendAsync(openAiRequest);
if (!openAiResponse.IsSuccessStatusCode)
{
    Console.WriteLine($"OpenAI request failed: {openAiResponse.StatusCode}");
    Console.WriteLine(await openAiResponse.Content.ReadAsStringAsync());
    return;
}

var openAiJson = await openAiResponse.Content.ReadAsStringAsync();

using var openAiDoc = JsonDocument.Parse(openAiJson);
if (!openAiDoc.RootElement.TryGetProperty("choices", out var choices) || 
    choices.GetArrayLength() == 0 ||
    !choices[0].TryGetProperty("message", out var message) ||
    !message.TryGetProperty("content", out var answerProperty))
{
    Console.WriteLine("Unexpected response format from OpenAI");
    Console.WriteLine($"Response: {openAiJson}");
    return;
}

var answer = answerProperty.GetString();

Console.WriteLine("\n GPT-4o Answer:\n");
Console.WriteLine(answer);
