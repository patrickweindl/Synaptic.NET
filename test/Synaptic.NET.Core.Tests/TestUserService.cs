using Synaptic.NET.Domain.Abstractions.Management;
using Synaptic.NET.Domain.Resources.Management;

namespace Synaptic.NET.Core.Tests;

public class TestUserService : ICurrentUserService
{
    private static readonly User s_testUser = new()
    {
        Id = Guid.Parse("4530bee0-3f17-4223-843d-e67c18f9fbfa"), DisplayName = "Test User", Identifier = "testUser"
    };
    public User GetCurrentUser()
    {
        return s_testUser;
    }
}
