using Synaptic.NET.Domain.Abstractions.Management;
using Synaptic.NET.Domain.Resources.Management;
using Synaptic.NET.Domain.Resources.Storage;

namespace Synaptic.NET.Core.Tests.Mocks;

public class MockUserService : ICurrentUserService
{
    private static readonly User s_testUser = new()
    {
        Id = Guid.Parse("4530bee0-3f17-4223-843d-e67c18f9fbfa"),
        DisplayName = "Test User",
        Identifier = "testUser"
    };
    public User GetCurrentUser()
    {
        return s_testUser;
    }

    public static readonly MemoryStore MockMemoryStore = new()
    {
        StoreId = Guid.Parse("4530bee1-3f17-4223-843d-e67c18f9fbfa"),
        Title = "test-memory-store",
        Description = "A memory store for testing purposes",
        Tags = ["Testing", "Unit Test"],
        OwnerUser = s_testUser,
        UserId = s_testUser.Id
    };
}
