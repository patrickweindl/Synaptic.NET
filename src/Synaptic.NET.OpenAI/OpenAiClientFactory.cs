using Microsoft.Extensions.Configuration;
using Synaptic.NET.OpenAI.Clients;

namespace Synaptic.NET.OpenAI;

public class OpenAiClientFactory
{
    private readonly Gpt4oClient _gpt4oClient;
    private readonly Gpt4oMiniClient _gpt4oMiniClient;
    private readonly Gpt41Client _gpt41Client;
    private readonly Gpt41miniClient _gpt41MiniClient;
    private readonly Gpt5Client _gpt5Client;
    private readonly Gpt5MiniClient _gpt5MiniClient;
    private readonly Gpt5NanoClient _gpt5NanoClient;

    public OpenAiClientFactory(IConfiguration configuration)
        : this(configuration["OpenAI:ApiKey"] ?? string.Empty)
    {
    }

    public OpenAiClientFactory(string openAiApiKey)
    {
        if (string.IsNullOrEmpty(openAiApiKey) || !openAiApiKey.StartsWith("sk-"))
        {
            throw new ArgumentException("Invalid OpenAI API key provided. It should start with 'sk-' and be non-empty.", nameof(openAiApiKey));
        }

        _gpt4oClient = new(openAiApiKey);
        _gpt4oMiniClient = new(openAiApiKey);
        _gpt41Client = new(openAiApiKey);
        _gpt41MiniClient = new(openAiApiKey);
        _gpt5Client = new(openAiApiKey);
        _gpt5MiniClient = new(openAiApiKey);
        _gpt5NanoClient = new(openAiApiKey);
    }

    public GptClientBase GetClient(string model)
    {
        string sanitizedModelName = model.ToLowerInvariant().Replace(" ", "").Replace(".", "").Replace("-", "");
        return model switch
        {
            _ when sanitizedModelName.Contains("5") && sanitizedModelName.Contains("nano") => _gpt5NanoClient,
            _ when sanitizedModelName.Contains("5") && sanitizedModelName.Contains("mini") => _gpt5MiniClient,
            _ when sanitizedModelName.Contains("5") => _gpt5Client,
            _ when sanitizedModelName.Contains("41") && sanitizedModelName.Contains("mini") => _gpt41MiniClient,
            _ when sanitizedModelName.Contains("41") => _gpt41Client,
            _ when sanitizedModelName.Contains("4o") && sanitizedModelName.Contains("mini") => _gpt4oMiniClient,
            _ => _gpt5Client
        };
    }

    public Gpt4oClient GetGpt4oClient() => _gpt4oClient;
    public Gpt4oMiniClient GetGpt4oMiniClient() => _gpt4oMiniClient;
    public Gpt41Client GetGpt41Client() => _gpt41Client;
    public Gpt41miniClient GetGpt41MiniClient() => _gpt41MiniClient;
    public Gpt5Client GetGpt5Client() => _gpt5Client;
    public Gpt5MiniClient GetGpt5MiniClient() => _gpt5MiniClient;
    public Gpt5NanoClient GetGpt5NanoClient() => _gpt5NanoClient;
}
