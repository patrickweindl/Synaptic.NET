using System.ClientModel;
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

    public override async Task<ClientResult<ChatCompletion>> CompleteChatAsync(IEnumerable<ChatMessage> messages,
        ChatCompletionOptions? options = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var messageList = messages.ToList();
        try
        {
            return await base.CompleteChatAsync(messageList, options, cancellationToken);
        }
        catch (ClientResultException)
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    return await base.CompleteChatAsync(messageList, options, cancellationToken);
                }
                catch (ClientResultException)
                {
                    await Task.Delay(i * 200);
                }
            }
        }

        return await base.CompleteChatAsync(messageList, options, cancellationToken);
    }
}
