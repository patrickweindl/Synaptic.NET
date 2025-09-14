using System.Text;
using Synaptic.NET.Domain.Constants;
using Synaptic.NET.Domain.Helpers;
using Synaptic.NET.Domain.Resources.Storage;
using Tiktoken;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;
using UglyToad.PdfPig.Writer;

namespace Synaptic.NET.Domain.Chunkers;

/// <summary>
/// A utility class for processing and chunking PDF documents into manageable segments
/// for downstream usage, such as semantic analysis or text processing.
/// </summary>
public static class SemanticPdfChunker
{
    private static readonly string s_tokenEncodingModel = "gpt-4o";
    private static readonly Tiktoken.Encoder s_encoder = ModelToEncoder.For(s_tokenEncodingModel);

    /// <summary>
    /// Divides a PDF file, represented as a base64-encoded string, into smaller text chunks
    /// suitable for processing, while adhering to specified token constraints.
    /// </summary>
    /// <param name="base64EncodedPdf">The PDF file represented as a base64-encoded string.</param>
    /// <param name="fileName">The PDF file name.</param>
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
    /// A list of <see cref="IngestionReference"/> objects representing the processed PDF content
    /// broken into chunks adhering to the specified constraints.
    /// </returns>
    public static List<IngestionReference> ChunkPdf(
        string base64EncodedPdf,
        string fileName,
        int minTokensPerChunk = SemanticConstants.AverageTokensPerPage * 3,
        int maxTokensPerChunk = SemanticConstants.AverageTokensPerPage * 7,
        int overlapTokens = SemanticConstants.AverageTokensPerPage / 2)
    {
        byte[] pdfBytes = Convert.FromBase64String(base64EncodedPdf);
        using var ms = new MemoryStream(pdfBytes);
        using var doc = PdfDocument.Open(ms);
        List<IngestionReference> chunks = new();
        var pages = new List<(Page Page, int PageIndex, List<TextBlock> TextBlocks)>();

        foreach (var page in doc.GetPages())
        {
            IReadOnlyList<Letter> letters = page.Letters;
            NearestNeighbourWordExtractor? wordExtractor = NearestNeighbourWordExtractor.Instance;
            IEnumerable<Word> words = wordExtractor.GetWords(letters);
            DocstrumBoundingBoxes? pageSegmenter = DocstrumBoundingBoxes.Instance;
            IReadOnlyList<TextBlock>? textBlocks = pageSegmenter.GetBlocks(words);
            UnsupervisedReadingOrderDetector? readingOrder = UnsupervisedReadingOrderDetector.Instance;
            IEnumerable<TextBlock>? orderedTextBlocks = readingOrder.Get(textBlocks);

            pages.Add((page, page.Number, orderedTextBlocks.ToList()));
        }
        int currentChunkTokens = 0;
        int chunkStartPage = pages.First().PageIndex;
        int lastPage = chunkStartPage;

        IngestionReference? lastChunk = null;
        foreach (var (page, index, textblocks) in pages)
        {
            _ = ProcessPage(page, textblocks, out int pageTokens);
            bool isInTargetRange = currentChunkTokens >= minTokensPerChunk && currentChunkTokens + pageTokens < maxTokensPerChunk;

            if (isInTargetRange)
            {
                var overlap = GetPdfOverlap(overlapTokens, chunkStartPage, lastPage, pages);

                string base64EncodedChunkPortion;
                var destStream = new MemoryStream();
                using (var destDoc = new PdfDocumentBuilder(destStream, disposeStream: true))
                {
                    int startIndex = overlap.NegativeOverlap.OverlapPageIndex < chunkStartPage ? overlap.NegativeOverlap.OverlapPageIndex : chunkStartPage;
                    int endIndex = overlap.PositiveOverlap.OverlapPageIndex > lastPage ? overlap.PositiveOverlap.OverlapPageIndex : lastPage;
                    doc.CopyPagesTo(startIndex, endIndex, destDoc);
                    byte[] output = destDoc.Build();
                    base64EncodedChunkPortion = Convert.ToBase64String(output);
                }

                var reference = new IngestionReference
                {
                    OriginalText = base64EncodedChunkPortion,
                    ReferenceName = fileName,
                    StartPage = chunkStartPage,
                    EndPage = lastPage
                };
                chunks.Add(reference);

                chunkStartPage = index;
                currentChunkTokens = 0;

                lastChunk = null;
            }

            currentChunkTokens += pageTokens;
            lastPage = index;
            var carryoverStream = new MemoryStream();
            string encodedString;
            using (var destDoc = new PdfDocumentBuilder(carryoverStream, disposeStream: true))
            {
                int startIndex = chunkStartPage;
                int endIndex = lastPage;
                doc.CopyPagesTo(startIndex, endIndex, destDoc);
                byte[] output = destDoc.Build();
                encodedString = Convert.ToBase64String(output);
            }
            lastChunk = new IngestionReference
            {
                OriginalText = encodedString,
                ReferenceName = fileName,
                StartPage = chunkStartPage,
                EndPage = lastPage
            };
        }

        // Document was too small to be chunked.
        if (chunks.Count == 0)
        {
            chunks.Add(new IngestionReference
            {
                OriginalText = base64EncodedPdf,
                ReferenceName = fileName,
                StartPage = 1,
                EndPage = doc.NumberOfPages
            });
        }

        if (lastChunk != null)
        {
            chunks.Add(lastChunk);
        }

        return chunks;
    }

    private static (string Text, List<string> Base64Images) ProcessPage(Page page, List<TextBlock> textBlocks, out int pageTokens)
    {
        StringBuilder text = new();
        List<string> base64Images = new();
        foreach (var textblock in textBlocks)
        {
            text.Append(textblock.Text.Trim());
            text.Append(Environment.NewLine);
        }

        foreach (var image in page.GetImages())
        {
            if (image.TryGetPng(out var pngBytes))
            {
                base64Images.Add(Convert.ToBase64String(pngBytes));
            }
        }
        pageTokens = s_encoder.CountTokens(text.ToString());
        pageTokens += base64Images.Count * SemanticConstants.TokenPenaltyPerImage;
        return (text.ToString(), base64Images);
    }

    private static (Overlap NegativeOverlap, Overlap PositiveOverlap) GetPdfOverlap(int overlapTokens, int firstPage, int lastPage, List<(Page Page, int PageIndex, List<TextBlock> TextBlocks)> source)
    {
        (string NegativeOverlapText, List<string> NegativeOverlapImages) negativeOverlap = (string.Empty, new());
        int negativePageIndex = firstPage - 1;
        if (firstPage >= 1)
        {
            // Page indices start at 1.
            int overallNegativeOverlapTokens = 0;
            do
            {
                var result = ProcessPage(source[negativePageIndex].Page, source[negativePageIndex].TextBlocks, out int negativeOverlapTokens);
                overallNegativeOverlapTokens += negativeOverlapTokens;
                negativePageIndex -= 1;
                negativeOverlap.NegativeOverlapText += result.Text;
                negativeOverlap.NegativeOverlapImages.AddRange(result.Base64Images);
            } while (negativePageIndex >= 0 && overallNegativeOverlapTokens < overlapTokens);
        }

        negativePageIndex = negativePageIndex - 1 < 1 ? 1 : negativePageIndex - 1;
        Overlap negativeOverlapResult = new() { OverlapPageIndex = negativePageIndex + 1, Text = negativeOverlap.NegativeOverlapText, Images = negativeOverlap.NegativeOverlapImages };
        (string PositiveOverlapText, List<string> PositiveOverlapImages) positiveOverlap = (PositiveOverlapText: string.Empty, new());

        int positivePageIndex = lastPage - 1;
        if (lastPage < source.Count - 1)
        {
            int overallPositiveOverlapTokens = 0;
            do
            {
                var result = ProcessPage(source[positivePageIndex].Page, source[positivePageIndex].TextBlocks, out int positiveOverlapTokens);
                overallPositiveOverlapTokens += positiveOverlapTokens;
                positivePageIndex += 1;
                positiveOverlap.PositiveOverlapText += result.Text;
                positiveOverlap.PositiveOverlapImages.AddRange(result.Base64Images);
            } while (positivePageIndex < source.Count - 1 && overallPositiveOverlapTokens < overlapTokens);
        }
        positivePageIndex = positivePageIndex + 1 > source.Max(s => s.PageIndex) ? source.Max(s => s.PageIndex) : positivePageIndex + 1;
        Overlap positiveOverlapResult = new() { OverlapPageIndex = positivePageIndex + 1, Text = positiveOverlap.PositiveOverlapText, Images = positiveOverlap.PositiveOverlapImages };
        return (negativeOverlapResult, positiveOverlapResult);
    }

    private class Overlap
    {
        public int OverlapPageIndex { get; set; }
        public string Text { get; set; } = string.Empty;
        public List<string> Images { get; set; } = new();
    }
}
