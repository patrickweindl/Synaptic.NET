using Microsoft.Extensions.Configuration;
using Synaptic.NET.Domain.Helpers;

namespace Synaptic.NET.Domain.Resources.Configuration;

public class OpenAiSettings
{
    private const string ApiKeyEnvVar = "OPENAI__APIKEY";
    private const string EmbeddingModelEnvVar = "OPENAI__EMBEDDINGMODEL";
    private const string MemoryRoutingModelEnvVar = "OPENAI__MEMORYROUTINGMODEL";
    private const string MemoryAugmentationModelEnvVar = "OPENAI__MEMORYAUGMENTATIONMODEL";
    private const string RagCreationModelEnvVar = "OPENAI__RAGCREATIONMODEL";
    private const string EmbeddingDimensionsEnvVar = "OPENAI__EMBEDDINGDIMENSIONS";
    public OpenAiSettings()
    {
        ApiKeyEnvVar.AssignValueFromEnvironmentVariableIfAvailable(s => ApiKey = s);
        EmbeddingModelEnvVar.AssignValueFromEnvironmentVariableIfAvailable(s => EmbeddingModel = s);
        MemoryRoutingModelEnvVar.AssignValueFromEnvironmentVariableIfAvailable(s => MemoryRoutingModel = s);
        MemoryAugmentationModelEnvVar.AssignValueFromEnvironmentVariableIfAvailable(s => MemoryAugmentationModel = s);
        RagCreationModelEnvVar.AssignValueFromEnvironmentVariableIfAvailable(s => RagCreationModel = s);
        EmbeddingDimensionsEnvVar.AssignValueFromEnvironmentVariableIfAvailable(s => EmbeddingDimensions = int.Parse(s));
    }

    public OpenAiSettings(IConfiguration configuration)
        : this()
    {
        if (configuration.GetSection("OpenAi").Exists())
        {
            var openAiSection = configuration.GetSection("OpenAi");
            openAiSection.AssignValueIfAvailable(s => ApiKey = s, "ApiKey");
            openAiSection.AssignValueIfAvailable(s => EmbeddingModel = s, "EmbeddingModel");
            openAiSection.AssignValueIfAvailable(s => MemoryRoutingModel = s, "MemoryRoutingModel");
            openAiSection.AssignValueIfAvailable(s => MemoryAugmentationModel = s, "MemoryAugmentationModel");
            openAiSection.AssignValueIfAvailable(s => EmbeddingDimensions = int.Parse(s), "EmbeddingDimensions");
            openAiSection.AssignValueIfAvailable(s => RagCreationModel = s, "RagCreationModel");
        }
    }
    public string EmbeddingModel { get; private set; } = "text-embedding-3-large";
    public string MemoryRoutingModel { get; private set; } = "gpt-4o";
    public string MemoryAugmentationModel { get; private set; } = "gpt-5-mini";
    public string RagCreationModel { get; private set; } = "gpt-5-mini";
    public string ApiKey { get; set; } = string.Empty;
    public int EmbeddingDimensions { get; private set; } = 3072;

}
