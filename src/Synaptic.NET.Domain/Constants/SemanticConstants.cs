namespace Synaptic.NET.Domain.Constants;

public class SemanticConstants
{
    public const int AverageWordsPerPage = 300;
    public const int AverageCharactersPerWord = 5;
    public const int AverageCharactersPerPage = AverageWordsPerPage * AverageCharactersPerWord;
    public const int AverageTokensPerPage = AverageWordsPerPage * 3;

    public const int TokenPenaltyPerImage = 85;
}
