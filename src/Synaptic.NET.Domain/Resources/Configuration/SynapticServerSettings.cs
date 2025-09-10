using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Synaptic.NET.Domain.Helpers;

namespace Synaptic.NET.Domain.Resources.Configuration;

public class SynapticServerSettings
{
    private const string JwtKeyEnvVar = "JWT__KEY";
    private const string JwtLifetimeEnvVar = "SECURITY__TOKENLIFETIME";
    private const string EncryptionKeyEnvVar = "ENCRYPTION__MASTERKEY";
    private const string BackupPathEnvVar = "BACKUP__PATH";
    private const string BackupEnabledEnvVar = "BACKUP__ENABLE";

    private readonly IConfiguration? _configuration;
    private readonly string _generatedJwtKey;

    public SynapticServerSettings(IConfiguration config)
    {
        Log.Information("Creating server settings...");
        Log.Information("Loading env variables...");

        JwtKeyEnvVar.AssignValueFromEnvironmentVariableIfAvailable(s => JwtKey = s);
        JwtLifetimeEnvVar.AssignValueFromEnvironmentVariableIfAvailable(s => JwtTokenLifetime = TimeSpan.Parse(s));
        if (config.GetSection("Jwt").Exists())
        {
            var jwtSection = config.GetSection("Jwt");
            jwtSection.AssignValueIfAvailable(s => JwtKey = s, "Key");
        }

        if (config.GetSection("Security").Exists())
        {
            var securitySection = config.GetSection("Security");
            securitySection.AssignValueIfAvailable(s => JwtTokenLifetime = TimeSpan.Parse(s), "TokenLifetime");
        }

        if (string.IsNullOrEmpty(JwtKey))
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            JwtKey = Convert.ToBase64String(bytes);
            Log.Information($"Using generated JWT Key {JwtKey}.");
        }

        EncryptionKeyEnvVar.AssignValueFromEnvironmentVariableIfAvailable(s => EncryptionKey = s);
        if (config.GetSection("Encryption").Exists())
        {
            var encryptionSection = config.GetSection("Encryption");
            encryptionSection.AssignValueIfAvailable(s => EncryptionKey = s, "MasterKey");
        }
        if (string.IsNullOrEmpty(EncryptionKey))
        {
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            EncryptionKey = Convert.ToBase64String(bytes);
            Log.Warning($"Using generated encryption key {EncryptionKey}. YOU HAVE TO SAVE THIS KEY TO ACCESS ENCRYPTED DATA LATER!");
        }

        _configuration = config;


        OpenAiSettings = new OpenAiSettings(config);
        GitHubOAuthProviderSettings = new OAuthProviderSettings("GitHub", config);
        MicrosoftOAuthProviderSettings = new OAuthProviderSettings("Microsoft", config);
        GoogleOAuthProviderSettings = new OAuthProviderSettings("Google", config);

        ServerSettings = new AspNetServerSettings(config);

        Log.Information("Created server settings:");
        Log.Information($"Memory Augmentation Model: {OpenAiSettings.MemoryAugmentationModel}");
        Log.Information($"Memory Routing Model: {OpenAiSettings.MemoryRoutingModel}");
        Log.Information($"RAG Creation Model: {OpenAiSettings.RagCreationModel}");
        Log.Information($"Embedding Model: {OpenAiSettings.EmbeddingModel}");
        Log.Information($"JWT Issuer: {ServerSettings.JwtIssuer}");
        Log.Information($"Server URL: {ServerSettings.ServerUrl}");
        Log.Information($"Server Port: {ServerSettings.ServerPort}");
        Log.Information($"Qdrant URL: {ServerSettings.QdrantUrl}");
        Log.Information($"Postgres URL: {ServerSettings.PostgresUrl}");
        Log.Information($"Trusted Proxies: {string.Join(",", ServerSettings.KnownProxies)}");

        if (string.IsNullOrEmpty(OpenAiSettings.ApiKey))
        {
            Log.Warning("OpenAI API Key is not set. It is required for proper functionality!");
        }
    }
    public string BaseDataPath => Path.Join(AppDomain.CurrentDomain.BaseDirectory, "data");
    public OpenAiSettings OpenAiSettings { get; private set; }
    public AspNetServerSettings ServerSettings { get; private set; }
    public string JwtKey { get; private set; }
    public string EncryptionKey { get; private set; }
    public TimeSpan JwtTokenLifetime { get; private set; } = TimeSpan.FromHours(1);
    public OAuthProviderSettings GitHubOAuthProviderSettings { get; private set; }
    public OAuthProviderSettings GoogleOAuthProviderSettings { get; private set; }
    public OAuthProviderSettings MicrosoftOAuthProviderSettings { get; private set; }
}

