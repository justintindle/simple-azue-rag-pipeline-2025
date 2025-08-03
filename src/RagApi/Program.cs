using RagApi.Services;

namespace RagApi;
internal class Program
{
    private static void Main(string[] args)
    {
        DotNetEnv.Env.Load();

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        
        // Add environment variables configuration
        builder.Configuration.AddEnvironmentVariables();
        
        // Map environment variables to AppSettings
        builder.Services.Configure<AppSettings>(options =>
        {
            options.AzureOpenAIApiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
            options.AzureOpenAIEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
            options.AzureOpenAIDeployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT");
            options.AzureOpenAIApiVersion = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_VERSION");
            options.AzureSearchServiceName = Environment.GetEnvironmentVariable("AZURE_SEARCH_SERVICE_NAME");
            options.AzureSearchApiKey = Environment.GetEnvironmentVariable("AZURE_SEARCH_API_KEY");
            options.AzureSearchIndexName = Environment.GetEnvironmentVariable("AZURE_SEARCH_INDEX_NAME");
            options.AzureSearchApiVersion = Environment.GetEnvironmentVariable("AZURE_SEARCH_API_VERSION");
            options.AzureStorageAccountName = Environment.GetEnvironmentVariable("AZURE_STORAGE_ACCOUNT_NAME");
            options.AzureStorageAccountKey = Environment.GetEnvironmentVariable("AZURE_STORAGE_ACCOUNT_KEY");
            options.AzureStorageContainerName = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONTAINER_NAME");
            options.AzureRegion = Environment.GetEnvironmentVariable("AZURE_REGION");
            options.AzureTenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
            options.AzureSubscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
        });

        // Inject services
        builder.Services.AddHttpClient<IRagService, RagService>();


        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();

        app.MapControllers();

        app.Run();
    }
}