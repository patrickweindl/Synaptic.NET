namespace Synaptic.NET.Domain.Resources;

/// <summary>
/// A class representing the result of a file processing operation with observable progress and status message.
/// </summary>
public class FileProcessor
{
    public async Task ExecutePdfFile(string fileName, string filePath)
    {
        throw new NotImplementedException();
    }

    public async Task ExecutePdf(string fileName, string base64Pdf)
    {
        throw new NotImplementedException();
    }

    public async Task ExecuteTextFile(string fileName, string filePath)
    {
        throw new NotImplementedException();
    }

    public async Task ExecuteText(string fileName, string base64Text)
    {
        throw new NotImplementedException();
    }

    public event EventHandler OnStatusChanged;
    public string Message { get; private set; } = string.Empty;
    public bool Completed { get; private set; }
    public double Progress { get; private set; }
    public List<(string referenceName, Memory memory)> Result { get; private set; } = new();
    public string StoreDescription { get; private set; } = string.Empty;
}
