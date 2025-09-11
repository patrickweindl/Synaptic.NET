using Synaptic.NET.Domain.Abstractions.Management;
using Synaptic.NET.Domain.Resources.Management;
using Synaptic.NET.Domain.Resources.Storage;

namespace Synaptic.NET.Core.Tests.Mocks;

public class MockUserService : ICurrentUserService
{
    public MockUserService(Guid? overrideGuid = null, string? overrideDisplayName = null, string? overrideUserId = null)
    {
        if (overrideGuid != null && overrideDisplayName != null && overrideUserId != null)
        {
            _testUser.Id = overrideGuid.Value;
            _testUser.DisplayName = overrideDisplayName;
            _testUser.Identifier = overrideUserId;
        }
    }

    private User _testUser = new()
    {
        Id = Guid.Parse("4530bee0-3f17-4223-843d-e67c18f9fbfa"),
        DisplayName = "Test User",
        Identifier = "testUser"
    };

    public User GetCurrentUser()
    {
        return _testUser;
    }

    public void SetCurrentUser(User user)
    {
        _testUser = user;
    }

    public MemoryStore MockMemoryStore => new()
    {
        StoreId = Guid.Parse("4530bee1-3f17-4223-843d-e67c18f9fbfa"),
        Title = "test-memory-store",
        Description = "A memory store for testing purposes",
        Tags = ["Testing", "Unit Test"],
        OwnerUser = _testUser,
        UserId = _testUser.Id
    };
}
