using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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

    public SynapticDbContext(DbContextOptions<SynapticDbContext> options)
        : base(options)
    {
    }

    public void SetCurrentUser(User user)
    {
        CurrentUserId = user.Id;
        CurrentGroupIds.Clear();
        CurrentGroupIds.AddRange(user.Memberships.Select(m => m.GroupId));
    }

    public User? DbUser()
    {
        return Users.FirstOrDefault(u => u.Id == CurrentUserId);
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
            .HasKey(u => u.Id);

        modelBuilder.Entity<User>()
            .HasMany(u => u.ApiKeys)
            .WithOne(a => a.Owner)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasMany(u => u.Stores)
            .WithOne(s => s.OwnerUser)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Group>()
            .HasKey(g => g.Id);

        modelBuilder.Entity<Group>()
            .HasMany(g => g.Stores)
            .WithOne(s => s.OwnerGroup)
            .HasForeignKey(s => s.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MemoryStore>()
            .HasKey(m => m.StoreId);

        modelBuilder.Entity<MemoryStore>()
            .HasOne(m => m.OwnerUser)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Memory>()
            .HasOne(m => m.OwnerUser)
            .WithMany()
            .HasForeignKey(m => m.Owner)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Memory>()
            .HasOne(m => m.OwnerGroup)
            .WithMany()
            .HasForeignKey(m => m.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Memory>()
            .HasOne(m => m.Store)
            .WithMany(s => s.Memories)
            .HasForeignKey(m => m.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MemoryStore>()
            .HasQueryFilter(s =>
                CurrentUserId == Guid.Empty
                || s.UserId == CurrentUserId
                || (s.GroupId.HasValue && CurrentGroupIds.Contains(s.GroupId.Value))
            )
            .Property(p => p.Tags)
            .HasConversion(v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new());

        modelBuilder.Entity<Memory>()
            .HasQueryFilter(m =>
                CurrentUserId == Guid.Empty
                || m.Owner == CurrentUserId
                || (m.GroupId.HasValue && CurrentGroupIds.Contains(m.GroupId.Value))
            )
            .Property(p => p.Tags)
            .HasConversion(v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new());



    }
}
