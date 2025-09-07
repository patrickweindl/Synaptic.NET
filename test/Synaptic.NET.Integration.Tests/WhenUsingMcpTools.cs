using Synaptic.NET.Mcp.Tools;

namespace Synaptic.NET.Integration.Tests;

public class WhenUsingMcpTools
{
    private IntegrationTestBuilder _builder;
    public WhenUsingMcpTools()
    {
        _builder = new IntegrationTestBuilder();
    }

    [Fact]
    public async Task ShouldCreateMemoryThroughMcpTool()
    {
        Skip.If(_builder.ShouldSkipIntegrationTest());

        await MemoryCreation.CreateMemory(
            "Test Memory from MCP Tools",
            "Test Memory created from Unit Tests",
            "A short test memory that has been created by a unit test which uses the static MCP tools",
            false,
            [],
            _builder.CurrentUserService,
            _builder.MemoryProvider,
            _builder.StoreRouter);

        Assert.True(_builder.DbContext.MemoryStores.ToList().Count > 0);
        Assert.True(_builder.DbContext.MemoryStores.SelectMany(s => s.Memories).ToList().Count > 0);

        var searchResult = await _builder.MemoryProvider.SearchAsync("Test", 10, -1);
        Assert.True(searchResult.Any());
    }
}
