using System.ClientModel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI.Chat;
using Tiktoken;

namespace Synaptic.NET.OpenAI.Clients;

public abstract class GptClientBase : ChatClient, IDecoratedGptClient
{
    protected GptClientBase(string model, string apiKey)
        : base(model, apiKey)
    {
    }

    public abstract int ContextWindowSize { get; }
    public abstract int MaxOutputTokens { get; }
    public abstract decimal CostPerInputToken { get; }
    public abstract decimal CostPerOutputToken { get; }
    public abstract string ModelIdentifier { get; }

    public Encoder GetEncoder()
    {
        try
        {
            return ModelToEncoder.For(ModelIdentifier);
        }
        catch
        {
            return ModelToEncoder.For("gpt-4o");
        }
    }

    public bool SupportsTemperatureSetting()
    {
        return !ModelIdentifier.Contains("gpt-5");
    }

    public bool SupportsReasoningEffort()
    {
        return ModelIdentifier.Contains("gpt-5");
    }

    public override async Task<ClientResult<ChatCompletion>> CompleteChatAsync(IEnumerable<ChatMessage> messages,
        ChatCompletionOptions? options = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var messageList = messages.ToList();
        try
        {
            var result = await base.CompleteChatAsync(messageList, options, cancellationToken);
            return result;
        }
        catch (ClientResultException ex)
        {
            Log.Logger.Error(ex, $"[GPT Client] Error during chat completion with model {ModelIdentifier}");
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    return await base.CompleteChatAsync(messageList, options, cancellationToken);
                }
                catch (ClientResultException)
                {
                    await Task.Delay(i * 200, cancellationToken);
                }
            }
            throw;
        }
    }
}
