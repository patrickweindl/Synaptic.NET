using System.ClientModel;
using System.Text;
using System.Text.Json;
using OpenAI.Chat;

namespace Synaptic.NET.OpenAI.StructuredResponses;

public static class CompletionOptionsHelper
{
    /// <summary>
    /// Creates ChatCompletionOptions for any class implementing IStructuredResponseSchema
    /// </summary>
    /// <typeparam name="T">The structured response schema type</typeparam>
    /// <returns>Configured ChatCompletionOptions with JSON schema format</returns>
    public static ChatCompletionOptions CreateStructuredResponseOptions<T>() where T : IStructuredResponseSchema
    {
        var schemaName = JsonSchemaGenerator.GetSchemaName<T>();
        var jsonSchema = JsonSchemaGenerator.GenerateJsonSchema<T>();

        ChatCompletionOptions options = new();
        options.ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
            jsonSchemaFormatName: schemaName,
            jsonSchema: BinaryData.FromBytes(Encoding.UTF8.GetBytes(jsonSchema)),
            jsonSchemaIsStrict: true);
        return options;
    }

    public static T? ParseModelResponse<T>(ClientResult<ChatCompletion> chatCompletion)
    {
        var deserializedResponse = JsonSerializer.Deserialize<T>(
            chatCompletion.Value.Content[0].Text,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (deserializedResponse == null)
        {
            Log.Logger.Error($"Failed to deserialize model response: {chatCompletion.Value.Content[0].Text}.");
        }
        return deserializedResponse;
    }
}
