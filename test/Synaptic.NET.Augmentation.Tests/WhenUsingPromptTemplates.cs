namespace Synaptic.NET.Augmentation.Tests;

public class WhenUsingPromptTemplates
{
    [Fact]
    public void ShouldFindAllPromptTemplates()
    {
        Assert.NotEmpty(PromptTemplates.GetFileProcessingSystemPrompt());
        Assert.NotEmpty(PromptTemplates.GetMemorySummarySystemPrompt());
        Assert.NotEmpty(PromptTemplates.GetMemoryRouterSystemPrompt());
        Assert.NotEmpty(PromptTemplates.GetMemoryRouterUserPrompt("id", "content", "stores"));
        Assert.NotEmpty(PromptTemplates.GetStoreTitleSystemPrompt());
        Assert.NotEmpty(PromptTemplates.GetStoreTitleUserPrompt("description", "memories"));
        Assert.NotEmpty(PromptTemplates.GetStoreRouterSystemPrompt());
        Assert.NotEmpty(PromptTemplates.GetStoreRouterUserPrompt("query", "stores"));
        Assert.NotEmpty(PromptTemplates.GetVectorSearchSystemPrompt());
        Assert.NotEmpty(PromptTemplates.GetVectorSearchUserPrompt("query", "store description", "memories"));
    }
}
