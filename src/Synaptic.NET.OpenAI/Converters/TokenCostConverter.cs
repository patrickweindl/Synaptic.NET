using Synaptic.NET.Domain.Metrics;
using Synaptic.NET.OpenAI.Resources;

namespace Synaptic.NET.OpenAI.Converters;

public static class TokenCostConverter
{
    public static double ConvertToCostInDollar(this MetricsEvent<long> tokenEvent)
    {
        if (!tokenEvent.Operation.Contains("Token incurrence"))
        {
            return 0;
        }
        string model = tokenEvent.Operation.Split('|')[1].ToLowerInvariant().Replace("-", "").Replace(".", "");
        double inputCostPerToken = model switch
        {
            _ when model.Contains("4o") && model.Contains("mini") => TokenCost.Gpt4oMiniInputTokenCost,
            _ when model.Contains("4o") => TokenCost.Gpt4oInputTokenCost,
            _ when model.Contains("41") && model.Contains("mini") => TokenCost.Gpt41MiniInputTokenCost,
            _ when model.Contains("41") => TokenCost.Gpt41InputTokenCost,
            _ when model.Contains("5") && model.Contains("nano") => TokenCost.Gpt5NanoInputTokenCost,
            _ when model.Contains("5") && model.Contains("mini") => TokenCost.Gpt5MiniInputTokenCost,
            _ when model.Contains("5") => TokenCost.Gpt5InputTokenCost,
            _ => 0.2
        };
        double outputCostPerToken = model switch
        {
            _ when model.Contains("4o") && model.Contains("mini") => TokenCost.Gpt4oMiniOutputTokenCost,
            _ when model.Contains("4o") => TokenCost.Gpt4oOutputTokenCost,
            _ when model.Contains("41") && model.Contains("mini") => TokenCost.Gpt41MiniOutputTokenCost,
            _ when model.Contains("41") => TokenCost.Gpt41OutputTokenCost,
            _ when model.Contains("5") && model.Contains("nano") => TokenCost.Gpt5NanoOutputTokenCost,
            _ when model.Contains("5") && model.Contains("mini") => TokenCost.Gpt5MiniOutputTokenCost,
            _ when model.Contains("5") => TokenCost.Gpt5OutputTokenCost,
            _ => 5.0
        };
        if (tokenEvent.Operation.Contains("Input"))
        {
            return inputCostPerToken * tokenEvent.Value;
        }
        return outputCostPerToken * tokenEvent.Value;
    }
}
