public class AppSettings
{
    // Azure OpenAI
    public string? AzureOpenAIApiKey { get; set; }
    public string? AzureOpenAIEndpoint { get; set; }
    public string? AzureOpenAIDeployment { get; set; }
    public string? AzureOpenAIApiVersion { get; set; }

    // Azure Cognitive Search
    public string? AzureSearchServiceName { get; set; }
    public string? AzureSearchApiKey { get; set; }
    public string? AzureSearchIndexName { get; set; }
    public string? AzureSearchApiVersion { get; set; }

    // Azure Blob Storage
    public string? AzureStorageAccountName { get; set; }
    public string? AzureStorageAccountKey { get; set; }
    public string? AzureStorageContainerName { get; set; }

    // General Azure Settings
    public string? AzureRegion { get; set; }
    public string? AzureTenantId { get; set; }
    public string? AzureSubscriptionId { get; set; }

    // Computed properties for backward compatibility and convenience
    public string SearchServiceEndpoint => $"https://{AzureSearchServiceName}.search.windows.net";
    public string? OpenAIEndpoint => AzureOpenAIEndpoint;
    public string? OpenAIApiKey => AzureOpenAIApiKey;
    public string? OpenAIModel => AzureOpenAIDeployment;
    public string? SearchApiKey => AzureSearchApiKey;
    public string? SearchIndexName => AzureSearchIndexName;
}
