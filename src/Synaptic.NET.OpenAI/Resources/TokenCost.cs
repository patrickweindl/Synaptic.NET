namespace Synaptic.NET.OpenAI.Resources;

public static class TokenCost
{
    public const double Gpt41InputTokenCost = (double)2 / 1000000;
    public const double Gpt41OutputTokenCost = (double)8 / 1000000;
    public const double Gpt41MiniInputTokenCost = 0.4 / 1000000;
    public const double Gpt41MiniOutputTokenCost = 1.6 / 1000000;
    public const double Gpt4oMiniInputTokenCost = 0.15 / 1000000;
    public const double Gpt4oMiniOutputTokenCost = 0.6 / 1000000;
    public const double Gpt4oInputTokenCost = 2.5 / 1000000;
    public const double Gpt4oOutputTokenCost = 10.0 / 1000000;
    public const double Gpt5InputTokenCost = 1.25 / 1000000;
    public const double Gpt5OutputTokenCost = 10.0 / 1000000;
    public const double Gpt5MiniInputTokenCost = 0.25 / 1000000;
    public const double Gpt5MiniOutputTokenCost = 2.0 / 1000000;
    public const double Gpt5NanoInputTokenCost = 0.05 / 1000000;
    public const double Gpt5NanoOutputTokenCost = 0.4 / 1000000;
}
