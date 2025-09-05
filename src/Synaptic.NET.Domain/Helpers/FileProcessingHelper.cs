using UglyToad.PdfPig;
using UglyToad.PdfPig.Writer;

namespace Synaptic.NET.Core.Helpers;

public static class FileProcessingHelper
{
    public static List<(int start, int end)> CreateChunks(int fullSize, int chunkSize, int chunkOverlap)
    {
        List<(int start, int end)> chunks = new();
        for (int i = 0; i < fullSize; i += chunkSize)
        {
            int startIndex = i == 0 ? 0 : i - chunkOverlap;
            int end = i + chunkSize + chunkOverlap;
            end = end > fullSize ? fullSize : end;
            chunks.Add((startIndex, end));
        }

        return chunks;
    }

    public static List<PdfPageBuilder> CopyPagesTo(this PdfDocument srcDoc, int pageFrom, int pageTo, PdfDocumentBuilder destDoc)
    {
        var newPages = new List<PdfPageBuilder>();

        for (int i = pageFrom; i <= pageTo; i++)
        {
            var page = destDoc.AddPage(srcDoc, i);
            newPages.Add(page);
        }

        return newPages;
    }

    public static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }
        return name.Replace(" ", "_").ToLowerInvariant();
    }
}
