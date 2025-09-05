using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;

namespace Synaptic.NET.Domain.Resources.Configuration;

public class SynapticServerSettings
{
    private readonly IConfiguration? _configuration;
    private readonly string _generatedJwtKey;

    public SynapticServerSettings(IConfiguration config)
    {
        _configuration = config;
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        _generatedJwtKey = Convert.ToBase64String(bytes);


        OpenAiApiKey = config["OpenAi:ApiKey"] ?? string.Empty;

        KnownProxies = string.IsNullOrEmpty(_configuration["Servers:KnownProxies"]) ? new List<string>() : _configuration["Servers:KnownProxies"]?.Split(',').ToList() ?? new();

        OpenAiRagCreationModel = _configuration?["OpenAi:RagCreationModel"] ?? "gpt-5-mini";
        OpenAiMemoryRoutingModel = _configuration?["OpenAi:MemoryRoutingModel"] ?? "gpt-5-mini";
        OpenAiTaskAugmentationModel = _configuration?["OpenAi:TaskAugmentationModel"] ?? "gpt-4o";
        OpenAiMemoryAugmentationModel = _configuration?["OpenAi:MemoryAugmentationModel"] ?? "gpt-5-mini";

        Log.Information("Created server settings:");
        Log.Information($"Task Augmentation Model: {OpenAiTaskAugmentationModel}");
        Log.Information($"Memory Augmentation Model: {OpenAiMemoryAugmentationModel}");
        Log.Information($"Memory Routing Model: {OpenAiMemoryRoutingModel}");
        Log.Information($"RAG Creation Model: {OpenAiRagCreationModel}");
        Log.Information($"Embedding Model: {OpenAiEmbeddingModel}");
        Log.Information($"JWT Issuer: {JwtIssuer}");
        Log.Information($"Backup Enabled: {EnableBackup}");
        Log.Information($"Server URL: {ServerUrl}");
        Log.Information($"Server Port: {ServerPort}");
        Log.Information($"Trusted Proxies: {string.Join(",", KnownProxies)}");
        Log.Information($"Trusting internal requests: {TrustInternalRequests}");

        if (string.IsNullOrEmpty(OpenAiApiKey))
        {
            Log.Warning("OpenAI API Key is not set. It is required for proper functionality!");
        }


    }
    public string BaseDataPath => Path.Join(AppDomain.CurrentDomain.BaseDirectory, "data");
    public string OpenAiTaskAugmentationModel { get; set; }
    public string OpenAiMemoryAugmentationModel { get; set; }
    public string OpenAiMemoryRoutingModel { get; set; }
    public string OpenAiRagCreationModel { get; set; }
    public string OpenAiEmbeddingModel => _configuration?["OpenAi:EmbeddingModel"] ?? "text-embedding-3-large";
    public string OpenAiAugmentedSearchModel => _configuration?["OpenAi:AugmentedSearchModel"] ?? "gpt-5-mini";
    public string OpenAiApiKey { get; set; }
    public string JwtKey => _configuration?["Jwt:Key"] ?? _generatedJwtKey;
    public string JwtIssuer => _configuration?["Jwt:Issuer"] ?? "mneme";
    public TimeSpan JwtTokenLifetime => string.IsNullOrEmpty(_configuration?["Security:TokenLifetime"]) ? TimeSpan.FromHours(1) : TimeSpan.Parse(_configuration["Security:TokenLifetime"]!);
    public bool TrustInternalRequests => !string.IsNullOrEmpty(_configuration?["Security:TrustInternal"]) && bool.Parse(_configuration["Security:TrustInternal"] ?? "False");
    public bool EnableBackup => !string.IsNullOrEmpty(_configuration?["Backup:Enable"]) && bool.Parse(_configuration["Backup:Enable"] ?? "False");
    public string BackupPath => _configuration?["Backup:Path"] ?? string.Empty;
    public string ServerUrl => _configuration?["Servers:Url"] ?? "127.0.0.1";
    public List<string> KnownProxies { get; }
    public int ServerPort => string.IsNullOrEmpty(_configuration?["Servers:Port"]) ? 8000 : int.Parse(_configuration["Servers:Port"]!);
    public List<string> AdminIdentifiers => string.IsNullOrEmpty(_configuration?["Security:Admins"]) ? new List<string>() : _configuration["Security:Admins"]?.Split(',').ToList() ?? new();
    public string QdrantServerUrl => _configuration?["Servers:QdrantUrl"] ?? "http://localhost";

    public OAuthSettings GitHubOAuthSettings => new()
    {
        Enabled = !string.IsNullOrEmpty(_configuration?["OAuth:GitHub:Enable"]) && bool.Parse(_configuration["OAuth:GitHub:Enable"] ?? "False"),
        ClientId = _configuration?["OAuth:GitHub:ClientId"] ?? string.Empty,
        ClientSecret = _configuration?["OAuth:GitHub:ClientSecret"] ?? string.Empty,
        OAuthUrl = "https://github.com/login/oauth/authorize"
    };

    public OAuthSettings GoogleOAuthSettings => new()
    {
        Enabled =
            !string.IsNullOrEmpty(_configuration?["OAuth:Google:Enable"]) &&
            bool.Parse(_configuration["OAuth:Google:Enable"] ?? "False"),
        ClientId = _configuration?["OAuth:Google:ClientId"] ?? string.Empty,
        ClientSecret = _configuration?["OAuth:Google:ClientSecret"] ?? string.Empty,
        OAuthUrl = "https://accounts.google.com/o/oauth2/v2/auth"
    };

    public OAuthSettings MicrosoftOAuthSettings => new()
    {
        Enabled =
            !string.IsNullOrEmpty(_configuration?["OAuth:Microsoft:Enable"]) &&
            bool.Parse(_configuration["OAuth:Microsoft:Enable"] ?? "False"),
        ClientId = _configuration?["OAuth:Microsoft:ClientId"] ?? string.Empty,
        ClientSecret = _configuration?["OAuth:Microsoft:ClientSecret"] ?? string.Empty,
        OAuthUrl = "https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize"
    };
}

