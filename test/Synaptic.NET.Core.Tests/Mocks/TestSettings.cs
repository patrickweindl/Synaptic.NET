using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Synaptic.NET.Domain.Resources.Configuration;

namespace Synaptic.NET.Core.Tests;

public class TestSettings : SynapticServerSettings
{
    public TestSettings()
        : base(new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build())
    { }

    public static TestSettings FromFile()
    {
        TestSettings? fileSettings = null;
        if (File.Exists(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "testSettings.json")))
        {
            fileSettings = JsonSerializer.Deserialize<TestSettings>(File.ReadAllText(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "testSettings.json")));
        }


        var testSettings = new TestSettings();
        if (!string.IsNullOrEmpty(fileSettings?.OpenAiTestApiKey) && fileSettings.OpenAiTestApiKey != "sk-...")
        {
            testSettings.OpenAiSettings.ApiKey = testSettings.OpenAiTestApiKey;
        }

        if (!string.IsNullOrEmpty(fileSettings?.QdrantTestUrl))
        {
            testSettings.ServerSettings.QdrantUrl = fileSettings.QdrantTestUrl;
        }

        return testSettings;
    }

    [JsonPropertyName("qdrantTestUrl")]
    public string QdrantTestUrl { get; set; } = string.Empty;

    [JsonPropertyName("openAiTestApiKey")]
    public string OpenAiTestApiKey { get; set; } = string.Empty;
}
