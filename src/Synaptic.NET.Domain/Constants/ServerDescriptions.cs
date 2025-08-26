namespace Synaptic.NET.Domain.Constants;

public static class ServerDescriptions
{
    public const string NewLine = "\r\n";

    public const string Tab = "\t";

    public const string McpServerName = "Synaptic MCP Server";

    public const string McpServerTitle = "Synaptic Model Context Protocol Server";

    public const string SynapticServerDescription =
        $"Synaptic MCP Server is a server that implements the Model Context Protocol (MCP) " +
        $"for managing and interacting with model contexts in the Synaptic ecosystem - which is a .NET based " +
        $"memory management system, that relies on a RAG/VectorDB setup to retrieve contextual information.{NewLine}" +
        $"Specific tools should be called initially after establishing a connection to the server to set up the initial context. These are:{NewLine}" +
        $"{Tab} - {ToolConstants.GetCurrentlyRelevantMemoriesToolName}";
}
