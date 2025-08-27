namespace Synaptic.NET.Core.BackgroundTasks;

public class FileUploadResult
{
    public string FileName { get; set; } = string.Empty;
    public int MemoryCount { get; set; }
    public string StoreIdentifier { get; set; } = string.Empty;
    public string StoreDescription { get; set; } = string.Empty;
}
