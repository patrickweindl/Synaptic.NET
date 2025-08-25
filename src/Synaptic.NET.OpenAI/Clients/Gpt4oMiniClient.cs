using Synaptic.NET.OpenAI.Resources;

namespace Synaptic.NET.OpenAI.Clients;

public sealed class Gpt4oMiniClient : GptClientBase
{
    public Gpt4oMiniClient(string openAiApiKey)
        : base("gpt-4o-mini", openAiApiKey)
    {
        if (string.IsNullOrEmpty(openAiApiKey) || !openAiApiKey.StartsWith("sk-"))
        {
            throw new ArgumentException("Invalid OpenAI API key provided. It should start with 'sk-' and be non-empty.", nameof(openAiApiKey));
        }
    }
    public override string ModelIdentifier => "gpt-4o-mini";
    public override int ContextWindowSize => 1280000;
    public override int MaxOutputTokens => 16384;
    public override decimal CostPerInputToken { get; } = Convert.ToDecimal(TokenCost.Gpt4oMiniInputTokenCost);
    public override decimal CostPerOutputToken { get; } = Convert.ToDecimal(TokenCost.Gpt4oMiniOutputTokenCost);
}
