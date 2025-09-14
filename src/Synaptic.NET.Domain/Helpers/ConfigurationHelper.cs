using Microsoft.Extensions.Configuration;

namespace Synaptic.NET.Domain.Helpers;

public static class ConfigurationHelper
{

    public static void AssignValueFromEnvironmentVariableIfAvailable(this string environmentVariableName,
        Action<string> assignment)
    {
        string? environmentValue = Environment.GetEnvironmentVariable(environmentVariableName);
        if (environmentValue != null)
        {
            assignment.Invoke(environmentValue);
        }
    }

    public static void AssignValueIfAvailable(this IConfigurationSection section, Action <string> assignment, string configurationKey)
    {
        string? configurationValue = section.GetValue<string>(configurationKey);
        if (configurationValue != null)
        {
            assignment.Invoke(configurationValue);
        }
    }
}
