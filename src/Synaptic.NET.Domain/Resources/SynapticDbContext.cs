using Microsoft.EntityFrameworkCore;
using Synaptic.NET.Domain.Abstractions.Management;
using Synaptic.NET.Domain.Resources.Management;
using Synaptic.NET.Domain.Resources.Storage;

namespace Synaptic.NET.Domain.Resources;

public class SynapticDbContext : DbContext
{
    public Guid CurrentUserId { get; set; } = Guid.Empty;
    public List<Guid> CurrentGroupIds { get; } = new();
    public DbSet<User> Users => Set<User>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<MemoryStore> MemoryStores => Set<MemoryStore>();
    public DbSet<Memory> Memories => Set<Memory>();

    public SynapticDbContext(DbContextOptions<SynapticDbContext> options, ICurrentUserService? currentUserService = null)
        : base(options)
    {
        if (currentUserService == null)
        {
            return;
        }

        var u = currentUserService.GetCurrentUser();
        CurrentUserId = u.Id;
        CurrentGroupIds.AddRange(u.Memberships.Select(m => m.GroupId));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GroupMembership>()
            .HasKey(gm => new { gm.UserId, gm.GroupId });

        modelBuilder.Entity<GroupMembership>()
            .HasOne(gm => gm.User)
            .WithMany(u => u.Memberships)
            .HasForeignKey(gm => gm.UserId);

        modelBuilder.Entity<GroupMembership>()
            .HasOne(gm => gm.Group)
            .WithMany(g => g.Memberships)
            .HasForeignKey(gm => gm.GroupId);

        modelBuilder.Entity<User>()
            .HasMany(u => u.Stores)
            .WithOne(s => s.OwnerUser)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Group>()
            .HasMany(g => g.Stores)
            .WithOne(s => s.OwnerGroup)
            .HasForeignKey(s => s.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MemoryStore>()
            .HasQueryFilter(s =>
                CurrentUserId == Guid.Empty
                || s.UserId == CurrentUserId
                || (s.GroupId.HasValue && CurrentGroupIds.Contains(s.GroupId.Value))
            );

        modelBuilder.Entity<Memory>()
            .HasQueryFilter(m =>
                CurrentUserId == Guid.Empty
                || m.Owner == CurrentUserId
                || (m.GroupId.HasValue && CurrentGroupIds.Contains(m.GroupId.Value))
            );

        modelBuilder.Entity<Memory>().HasIndex(m => m.Owner);
        modelBuilder.Entity<Memory>().HasIndex(m => m.GroupId);
        modelBuilder.Entity<Memory>().HasIndex(m => m.StoreId);

        modelBuilder.Entity<MemoryStore>().HasIndex(s => s.UserId);
        modelBuilder.Entity<MemoryStore>().HasIndex(s => s.GroupId);
    }
}
