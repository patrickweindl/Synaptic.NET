using Synaptic.NET.AppHost;

namespace Synaptic.NET.ExampleConsumer;

public static class ExampleProgram
{
    public static void Main(string[] args)
    {
        IConfiguration overrideConfig = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: true).AddEnvironmentVariables().Build();

        IEnumerable<Type> additionalToolTypes = new[]
        {
            typeof(ExampleMcpTools.InjectedMcpTool),
            typeof(ExampleMcpTools.InjectedMcpToolWithDi)
        };

        var app = SynapticAppHost.DefaultSynapticApplication(args, false, overrideConfig,
            additionalToolTypes: additionalToolTypes);
        app.RunDefaultApplication();
    }
}
