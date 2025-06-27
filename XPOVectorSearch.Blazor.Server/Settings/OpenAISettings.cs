namespace XPOVectorSearch.Blazor.Server.Settings;

public class OpenAISettings
{
    public required string ApiKey { get; init; }
    public required string ChatCompletionModelId { get; init; }
    public required string EmbeddingModelId { get; init; }
    public int? EmbeddingDimensions { get; set; }
}