using Synaptic.NET.OpenAI.Resources;

namespace Synaptic.NET.OpenAI.Clients;

/// <summary>
/// A client for the GPT-5 model.
/// Useful for: Complex reasoning, broad world knowledge, and code-heavy or multi-step agentic tasks.
/// </summary>
public sealed class Gpt5Client : GptClientBase
{
    public Gpt5Client(string openAiApiKey)
        : base("gpt-5", openAiApiKey)
    {
        if (string.IsNullOrEmpty(openAiApiKey) || !openAiApiKey.StartsWith("sk-"))
        {
            throw new ArgumentException("Invalid OpenAI API key provided. It should start with 'sk-' and be non-empty.", nameof(openAiApiKey));
        }
    }
    public override string ModelIdentifier => "gpt-5";
    public override int ContextWindowSize => 400000;
    public override int MaxOutputTokens => 128000;
    public override decimal CostPerInputToken { get; } = Convert.ToDecimal(TokenCost.Gpt5InputTokenCost);
    public override decimal CostPerOutputToken { get; } = Convert.ToDecimal(TokenCost.Gpt5OutputTokenCost);
}
