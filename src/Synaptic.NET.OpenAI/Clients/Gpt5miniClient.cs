using Synaptic.NET.OpenAI.Resources;

namespace Synaptic.NET.OpenAI.Clients;

/// <summary>
/// A client for the GPT-5 Mini model.
/// Useful for: Tasks that require a balance between performance and cost, such as summarization,
/// instruction-following, and general-purpose question answering.
/// </summary>
public sealed class Gpt5MiniClient : GptClientBase
{
    public Gpt5MiniClient(string openAiApiKey)
        : base("gpt-5-mini", openAiApiKey)
    {
        if (string.IsNullOrEmpty(openAiApiKey) || !openAiApiKey.StartsWith("sk-"))
        {
            throw new ArgumentException("Invalid OpenAI API key provided. It should start with 'sk-' and be non-empty.", nameof(openAiApiKey));
        }
    }
    public override string ModelIdentifier => "gpt-5-mini";
    public override int ContextWindowSize => 400000;
    public override int MaxOutputTokens => 128000;
    public override decimal CostPerInputToken { get; } = Convert.ToDecimal(TokenCost.Gpt5MiniInputTokenCost);
    public override decimal CostPerOutputToken { get; } = Convert.ToDecimal(TokenCost.Gpt5MiniOutputTokenCost);
}
