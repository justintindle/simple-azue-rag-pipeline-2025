namespace RagApi.Services;

public interface IRagService
{
    Task<string> GetRagResponseAsync(string question);
}
