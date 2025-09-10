using Microsoft.Extensions.Configuration;
using Synaptic.NET.Domain.Helpers;

namespace Synaptic.NET.Domain.Resources.Configuration;

public class AspNetServerSettings
{
    private const string ServerUrlEnvVar = "SERVERS__URL";
    private const string KnownProxiesEnvVar = "SERVERS__KNOWNPROXIES";
    private const string ServerPortEnvVar = "SERVERS__PORT";
    private const string QdrantUrlEvnVar = "SERVERS__QDRANTURL";
    private const string PostgresUrlEnvVar = "SERVERS__POSTGRESURL";
    private const string PostgresUserNameEnvVar = "SERVERS__POSTGRESUSERNAME";
    private const string PostgresPasswordEnvVar = "SERVERS__POSTGRESPASSWORD";
    private const string AdminIdentifiersEnvVar = "SECURITY__ADMINS";

    public AspNetServerSettings()
    {
        ServerUrlEnvVar.AssignValueFromEnvironmentVariableIfAvailable(s => ServerUrl = s);
        ServerPortEnvVar.AssignValueFromEnvironmentVariableIfAvailable(s => ServerPort = int.Parse(s));
        KnownProxiesEnvVar.AssignValueFromEnvironmentVariableIfAvailable(s => KnownProxies = s.Split(',').ToList());
        QdrantUrlEvnVar.AssignValueFromEnvironmentVariableIfAvailable(s => QdrantUrl = s);
        PostgresUrlEnvVar.AssignValueFromEnvironmentVariableIfAvailable(s => PostgresUrl = s);
        PostgresUserNameEnvVar.AssignValueFromEnvironmentVariableIfAvailable(s => PostgresUserName = s);
        PostgresPasswordEnvVar.AssignValueFromEnvironmentVariableIfAvailable(s => PostgresPassword = s);
        AdminIdentifiersEnvVar.AssignValueFromEnvironmentVariableIfAvailable(s => AdminIdentifiers = s.Split(',').ToList());
    }

    public AspNetServerSettings(IConfiguration configuration)
    {
        if (configuration.GetSection("Servers").Exists())
        {
            var serversSection = configuration.GetSection("Servers");
            serversSection.AssignValueIfAvailable(s => ServerUrl = s, "Url");
            serversSection.AssignValueIfAvailable(s => ServerPort = int.Parse(s), "Port");
            serversSection.AssignValueIfAvailable(s => KnownProxies = s.Split(',').ToList(), "KnownProxies");
            serversSection.AssignValueIfAvailable(s => QdrantUrl = s, "QdrantUrl");
            serversSection.AssignValueIfAvailable(s => PostgresUrl = s, "PostgresUrl");
            serversSection.AssignValueIfAvailable(s => PostgresUserName = s, "PostgresUserName");
            serversSection.AssignValueIfAvailable(s => PostgresPassword = s, "PostgresPassword");
        }

        if (configuration.GetSection("Security").Exists())
        {
            var securitySection = configuration.GetSection("Security");
            securitySection.AssignValueIfAvailable(s => AdminIdentifiers = s.Split(',').ToList(), "Admins");
        }
    }

    public string JwtIssuer => ServerUrl;
    public string ServerUrl { get; set; } = "https://localhost:8001";
    public int ServerPort { get; set; } = 8001;
    public List<string> KnownProxies { get; set;  } = new List<string>();
    public List<string> AdminIdentifiers { get; set;  } = new List<string>();
    public string QdrantUrl { get; set; } = "http://localhost:6334";
    public string PostgresUrl { get; set; } = "http://localhost:5432";
    public string PostgresUserName { get; set; } = "postgres";
    public string PostgresPassword { get; set; } = string.Empty;
}
