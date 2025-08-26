using Synaptic.NET.OpenAI.Resources;

namespace Synaptic.NET.OpenAI.Clients;

public sealed class Gpt41miniClient : GptClientBase
{
    public Gpt41miniClient(string openAiApiKey)
        : base("gpt-4.1-mini", openAiApiKey)
    {
        if (string.IsNullOrEmpty(openAiApiKey) || !openAiApiKey.StartsWith("sk-"))
        {
            throw new ArgumentException("Invalid OpenAI API key provided. It should start with 'sk-' and be non-empty.", nameof(openAiApiKey));
        }
    }

    public override string ModelIdentifier => "gpt-4.1-mini";
    public override int ContextWindowSize => 1047576;
    public override int MaxOutputTokens => 32768;
    public override decimal CostPerInputToken { get; } = Convert.ToDecimal(TokenCost.Gpt41MiniInputTokenCost);
    public override decimal CostPerOutputToken { get; } = Convert.ToDecimal(TokenCost.Gpt41MiniOutputTokenCost);
}
