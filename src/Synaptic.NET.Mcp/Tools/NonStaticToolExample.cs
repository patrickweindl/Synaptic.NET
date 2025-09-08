using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Synaptic.NET.Mcp.Tools;

/// <summary>
/// A non-static example tool that returns back a simple string constructed from the parameters.
/// </summary>
public class NonStaticToolExample : McpServerTool
{
    public override ValueTask<CallToolResult> InvokeAsync(RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken = new CancellationToken())
    {
        if (request.Params == null || request.Params?.Arguments?.Count == 0)
        {
            return new ValueTask<CallToolResult>(
                new CallToolResult
                {
                    IsError = true,
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock
                        {
                            Text = "No arguments found on the request."
                        }
                    }
                }
            );
        }

        string inputArgument = string.Empty;
        if (request.Params?.Arguments?.TryGetValue("input", out JsonElement inputJsonElement) ?? false)
        {
            if (inputJsonElement.ValueKind == JsonValueKind.String)
            {
                inputArgument = inputJsonElement.Deserialize<string>() ?? string.Empty;
            }
        }

        if (string.IsNullOrEmpty(inputArgument))
        {
            return new ValueTask<CallToolResult>(
                new CallToolResult
                {
                    IsError = true,
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock
                        {
                            Text = "The input string was empty."
                        }
                    }
                }
            );
        }

        var result = new CallToolResult
        {
            IsError = false,
            Content = new List<ContentBlock>
            {
                new TextContentBlock
                {
                    Text = $"Hello {inputArgument}"
                }
            }
        };

        ValueTask<CallToolResult> resultTask = new(result);
        return resultTask;
    }

    private static object InputSchema => new
    {
        type = "object",
        properties = new
        {
            input = new
            {
                type = "string",
                description = "The input string"
            }
        }
    };

    private static object OutputSchema => new
    {
        type = "object",
        properties = new
        {
            output = new
            {
                type = "string",
                description = "The output string"
            }
        }
    };

    public override Tool ProtocolTool { get; } = new()
    {
        Name = "NonStaticToolExample",
        Description = "Non static tool example that returns back a simple string constructed from the parameters.",
        Title = "NonStaticToolExample",
        Annotations = new ToolAnnotations
        {
            DestructiveHint = false,
            IdempotentHint = true,
            ReadOnlyHint = true,
            Title = "A non static example tool"
        },
        InputSchema = JsonSerializer.SerializeToElement(InputSchema),
        OutputSchema = JsonSerializer.SerializeToElement(OutputSchema)
    };
}
