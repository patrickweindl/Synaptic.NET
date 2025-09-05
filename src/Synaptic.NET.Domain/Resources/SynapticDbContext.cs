using Microsoft.EntityFrameworkCore;
using Synaptic.NET.Domain.Resources.Management;
using Synaptic.NET.Domain.Resources.Storage;

namespace Synaptic.NET.Domain.Resources;

public class SynapticDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMembership> GroupMemberships => Set<GroupMembership>();
    public DbSet<MemoryStore> MemoryStores => Set<MemoryStore>();
    public DbSet<Memory> Memories => Set<Memory>();

    public SynapticDbContext(DbContextOptions<SynapticDbContext> options)
        : base(options)
    { }

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
            .HasMany(s => s.Memories)
            .WithOne(m => m.Store)
            .HasForeignKey(m => m.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
