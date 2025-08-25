using Synaptic.NET.OpenAI.Clients;
using Synaptic.NET.OpenAI.Resources;

namespace mneme.OpenAi.Clients;

/// <summary>
/// A client for the GPT-5 Nano model.
/// Useful for: High-throughput tasks, especially simple instruction-following or classification.
/// </summary>
public sealed class Gpt5NanoClient : GptClientBase
{
    public Gpt5NanoClient(string openAiApiKey)
        : base("gpt-5-nano", openAiApiKey)
    {
        if (string.IsNullOrEmpty(openAiApiKey) || !openAiApiKey.StartsWith("sk-"))
        {
            throw new ArgumentException("Invalid OpenAI API key provided. It should start with 'sk-' and be non-empty.", nameof(openAiApiKey));
        }
    }

    public override string ModelIdentifier => "gpt-5-nano";
    public override int ContextWindowSize => 400000;
    public override int MaxOutputTokens => 128000;
    public override decimal CostPerInputToken { get; } = Convert.ToDecimal(TokenCost.Gpt5NanoInputTokenCost);
    public override decimal CostPerOutputToken { get; } = Convert.ToDecimal(TokenCost.Gpt5NanoOutputTokenCost);
}
