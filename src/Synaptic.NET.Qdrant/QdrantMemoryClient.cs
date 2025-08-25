using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using OpenAI;
using OpenAI.Embeddings;
using Qdrant.Client;

namespace Synaptic.NET.Qdrant;

public class QdrantMemoryClient
{
    public QdrantVectorStore Store { get; }
    private readonly EmbeddingClient _embeddingGenerator;
    private readonly QdrantClient _client;
    public QdrantMemoryClient(IConfiguration configuration)
    {
        _embeddingGenerator = new OpenAIClient(configuration["OpenAi:ApiKey"])
            .GetEmbeddingClient(configuration["OpenAi:EmbeddingModel"] ?? "text-embedding-3-large")
            ;
        _client = new QdrantClient(configuration["Servers:Qdrant:Ip"] ?? "localhost", configuration["Servers:Qdrant:Port"] == null ? 6334 : int.Parse(configuration["Servers:Qdrant:Port"]!));
        _ = Task.Run(async () => await _client.HealthAsync()).Result;
        Store = new QdrantVectorStore(
            _client,
            false, new QdrantVectorStoreOptions
            {
                EmbeddingGenerator = _embeddingGenerator.AsIEmbeddingGenerator(),
                HasNamedVectors = true
            });
    }
}
