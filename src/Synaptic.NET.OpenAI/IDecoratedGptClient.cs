using Tiktoken;

namespace Synaptic.NET.OpenAI;

public interface IDecoratedGptClient
{
    Encoder GetEncoder();
    string ModelIdentifier { get; }
    int ContextWindowSize { get; }
    int MaxOutputTokens { get; }

    /// <summary>
    /// The cost per input token in USD.
    /// </summary>
    decimal CostPerInputToken { get; }

    /// <summary>
    /// The cost per output token in USD.
    /// </summary>
    decimal CostPerOutputToken { get; }
}
