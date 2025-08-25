using Synaptic.NET.OpenAI.Resources;

namespace Synaptic.NET.OpenAI.Clients;

/// <summary>
/// A client for the GPT-4o model.
/// </summary>
public sealed class Gpt4oClient : GptClientBase
{
    public Gpt4oClient(string openAiApiKey)
        : base("gpt-4o", openAiApiKey)
    {
        if (string.IsNullOrEmpty(openAiApiKey) || !openAiApiKey.StartsWith("sk-"))
        {
            throw new ArgumentException("Invalid OpenAI API key provided. It should start with 'sk-' and be non-empty.", nameof(openAiApiKey));
        }
    }
    public override string ModelIdentifier => "gpt-4o";
    public override int ContextWindowSize => 128000;
    public override int MaxOutputTokens => 16384;
    public override decimal CostPerInputToken { get; } = Convert.ToDecimal(TokenCost.Gpt4oInputTokenCost);
    public override decimal CostPerOutputToken { get; } = Convert.ToDecimal(TokenCost.Gpt4oOutputTokenCost);
}
