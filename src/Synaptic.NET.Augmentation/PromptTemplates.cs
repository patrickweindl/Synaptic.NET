using System.Reflection;

namespace Synaptic.NET.Domain.Helpers;

public static class PromptTemplates
{
    private static string LoadPrompt(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        string resourceName = $"Prompts.{name.Replace(".", "/")}.txt";
        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            resourceName = $"Prompts.{name.Replace(".", "/")}.md";
            using Stream? markdownStream = assembly.GetManifestResourceStream(resourceName);
            if (markdownStream == null)
            {
                throw new InvalidOperationException($"Prompt template '{resourceName}' not found.");
            }
            using var markdownReader = new StreamReader(markdownStream);
            return markdownReader.ReadToEnd();
        }
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string Format(string template, params (string Key, string Value)[] values)
    {
        foreach (var (key, value) in values)
        {
            template = template.Replace($"{{{key}}}", value);
        }
        return template;
    }

    public static string GetFileProcessingSystemPrompt()
        => LoadPrompt("augmentation.file_processing_system");

    public static string GetStoreRouterSystemPrompt()
        => LoadPrompt("core.store_router_system");

    public static string GetStoreRouterUserPrompt(string query, string stores)
        => Format(LoadPrompt("core.store_router_user"), ("Query", query), ("Stores", stores));

    public static string GetMemoryRouterSystemPrompt()
        => LoadPrompt("core.memory_router_system");

    public static string GetMemoryRouterUserPrompt(string identifier, string content, string stores)
        => Format(LoadPrompt("core.store_router_user"), ("Identifier", identifier), ("Content", content), ("Stores", stores));

    public static string GetMemorySummarySystemPrompt()
        => LoadPrompt("augmentation.memory_summary_system");

    public static string GetStoreSummarySystemPrompt()
        => LoadPrompt("augmentation.store_summary_system");

    public static string GetStoreSummaryUserPrompt(string storeIdentifier, string memories)
        => Format(LoadPrompt("augmentation.store_summary_user"), ("StoreIdentifier", storeIdentifier), ("Memories", memories));

    public static string GetVectorSearchSystemPrompt()
        => LoadPrompt("core.vector_search_system");

    public static string GetVectorSearchUserPrompt(string query, string storeDescription, string memoryDescriptions)
        => Format(LoadPrompt("core.vector_search_user"),
            ("Query", query),
            ("StoreDescription", storeDescription),
            ("MemoryDescriptions", memoryDescriptions));
}
