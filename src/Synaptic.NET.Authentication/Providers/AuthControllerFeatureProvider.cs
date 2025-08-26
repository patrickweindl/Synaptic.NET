using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Synaptic.NET.Authentication.Controllers;

namespace Synaptic.NET.Authentication.Providers;

public class AuthControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        var allowedControllers = new[]
        {
            typeof(AccountController),
            typeof(AuthController),
            typeof(WellKnownController)
        };

        feature.Controllers.Clear();
        foreach (var type in allowedControllers.Select(t => t.GetTypeInfo()))
        {
            feature.Controllers.Add(type);
        }
    }
}
