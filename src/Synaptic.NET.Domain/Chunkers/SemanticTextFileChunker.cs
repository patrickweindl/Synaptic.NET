using System.Text;
using Synaptic.NET.Domain.Constants;
using Synaptic.NET.Domain.Resources.Storage;
using Tiktoken;

namespace Synaptic.NET.Domain.Chunkers;

/// <summary>
/// A utility class for processing and chunking text documents into manageable segments
/// for downstream usage, such as semantic analysis or text processing.
/// </summary>
public static class SemanticTextFileChunker
{
    private static readonly char[] SentenceEnders = new[] { '.', '!', '?' };
    private static readonly string s_tokenEncodingModel = "gpt-4o";
    private static readonly Tiktoken.Encoder s_encoder = ModelToEncoder.For(s_tokenEncodingModel);

    /// <summary>
    /// Divides a text file, represented as a raw string, into smaller text chunks
    /// suitable for processing, while adhering to specified token constraints.
    /// </summary>
    /// <param name="rawString">The text file content as a raw string.</param>
    /// <param name="fileName">The text file name.</param>
    /// <param name="minTokensPerChunk">
    /// The minimum allowable tokens in a chunk. If a chunk cannot reach the target threshold,
    /// it will still be created if it exceeds this minimum value. Defaults to three pages worth
    /// of tokens.
    /// </param>
    /// <param name="maxTokensPerChunk">
    /// The maximum allowable tokens per chunk. If the content exceeds this threshold, the chunk
    /// will be split further. Defaults to seven pages worth of tokens.
    /// </param>
    /// <param name="overlapTokens">
    /// Number of overlapping tokens between consecutive chunks to maintain context.
    /// Defaults to one half of a page's worth, based on average tokens per page.
    /// </param>
    /// <returns>
    /// A list of <see cref="IngestionReference"/> objects representing the processed text content
    /// broken into chunks adhering to the specified constraints.
    /// </returns>
    public static List<IngestionReference> ChunkFile(
        string rawString,
        string fileName,
        int minTokensPerChunk = SemanticConstants.AverageTokensPerPage * 3,
        int maxTokensPerChunk = SemanticConstants.AverageTokensPerPage * 7,
        int overlapTokens = SemanticConstants.AverageTokensPerPage / 2)
    {
        if (string.IsNullOrWhiteSpace(rawString))
            return new List<IngestionReference>();

        List<IngestionReference> chunks = new();

        var sentences = SplitIntoSentences(rawString);

        int currentChunkTokens = 0;
        int chunkStartIndex = 0;
        var currentChunkText = new StringBuilder();

        IngestionReference? lastChunk = null;

        for (int i = 0; i < sentences.Count; i++)
        {
            string sentence = sentences[i];
            int sentenceTokens = s_encoder.CountTokens(sentence);

            bool isInTargetRange = currentChunkTokens >= minTokensPerChunk &&
                                   currentChunkTokens + sentenceTokens < maxTokensPerChunk;

            if (isInTargetRange && currentChunkTokens > 0)
            {
                var overlapText = GetTextOverlap(sentences, chunkStartIndex, i - 1, overlapTokens);
                string chunkContent = overlapText.NegativeOverlap + currentChunkText + overlapText.PositiveOverlap;

                var reference = new IngestionReference
                {
                    OriginalText = chunkContent,
                    ReferenceName = fileName,
                    StartPage = chunkStartIndex + 1,
                    EndPage = i
                };
                chunks.Add(reference);

                chunkStartIndex = i;
                currentChunkTokens = 0;
                currentChunkText.Clear();
                lastChunk = null;
            }

            currentChunkText.Append(sentence);
            if (i < sentences.Count - 1)
            {
                currentChunkText.Append(' ');
            }
            currentChunkTokens += sentenceTokens;

            string carryoverContent = currentChunkText.ToString();
            lastChunk = new IngestionReference
            {
                OriginalText = carryoverContent,
                ReferenceName = fileName,
                StartPage = chunkStartIndex + 1,
                EndPage = i + 1
            };
        }

        // If no chunks were created, add the entire text as a single chunk
        if (chunks.Count == 0)
        {
            chunks.Add(new IngestionReference
            {
                OriginalText = rawString,
                ReferenceName = fileName,
                StartPage = 1,
                EndPage = sentences.Count
            });
        }

        if (lastChunk != null)
        {
            chunks.Add(lastChunk);
        }

        return chunks;
    }

    private static List<string> SplitIntoSentences(string text)
    {
        var sentences = new List<string>();
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                continue;
            }

            var currentSentence = new StringBuilder();

            for (int i = 0; i < trimmedLine.Length; i++)
            {
                currentSentence.Append(trimmedLine[i]);

                if (!SentenceEnders.Contains(trimmedLine[i]))
                {
                    continue;
                }

                if (i != trimmedLine.Length - 1 && !char.IsWhiteSpace(trimmedLine[i + 1]))
                {
                    continue;
                }

                string sentence = currentSentence.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(sentence))
                {
                    sentences.Add(sentence);
                }
                currentSentence.Clear();
            }

            string remainingSentence = currentSentence.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(remainingSentence))
            {
                sentences.Add(remainingSentence);
            }
        }

        return sentences;
    }

    private static (string NegativeOverlap, string PositiveOverlap) GetTextOverlap(
        List<string> sentences,
        int startIndex,
        int endIndex,
        int overlapTokens)
    {
        int negativeOverlapTokens = 0;
        int negativeIndex = startIndex - 1;
        var negativeBuilder = new StringBuilder();

        while (negativeIndex >= 0 && negativeOverlapTokens < overlapTokens)
        {
            string sentence = sentences[negativeIndex];
            int sentenceTokens = s_encoder.CountTokens(sentence);

            if (negativeOverlapTokens + sentenceTokens <= overlapTokens)
            {
                negativeBuilder.Insert(0, sentence + " ");
                negativeOverlapTokens += sentenceTokens;
            }
            else
            {
                break;
            }

            negativeIndex--;
        }

        string negativeOverlap = negativeBuilder.ToString().Trim();

        int positiveOverlapTokens = 0;
        int positiveIndex = endIndex + 1;
        var positiveBuilder = new StringBuilder();

        while (positiveIndex < sentences.Count && positiveOverlapTokens < overlapTokens)
        {
            string sentence = sentences[positiveIndex];
            int sentenceTokens = s_encoder.CountTokens(sentence);

            if (positiveOverlapTokens + sentenceTokens <= overlapTokens)
            {
                positiveBuilder.Append(sentence + " ");
                positiveOverlapTokens += sentenceTokens;
            }
            else
            {
                break;
            }

            positiveIndex++;
        }

        string positiveOverlap = positiveBuilder.ToString().Trim();

        return (negativeOverlap, positiveOverlap);
    }
}
