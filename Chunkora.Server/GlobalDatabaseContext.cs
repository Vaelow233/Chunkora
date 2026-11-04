using Chunkora.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Chunkora.Server;

public class GlobalDatabaseContext(DbContextOptions<GlobalDatabaseContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    
    public DbSet<Role> Roles { get; set; }
    
    public DbSet<Node> Nodes { get; set; }
    
    public DbSet<NodeMetrics> NodeMetrics { get; set; }
    
    public DbSet<Instance> Instances { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Name).IsUnique();
        });
        modelBuilder.Entity<User>()
            .Property(user => user.RefreshToken)
            .IsConcurrencyToken();
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Name).IsUnique();
        });
        modelBuilder.Entity<Node>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Name).IsUnique();
        });
        modelBuilder.Entity<Node>()
            .HasIndex(x => x.AgentToken)
            .IsUnique();
        modelBuilder.Entity<NodeMetrics>(entity =>
        {
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<Instance>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Name).IsUnique();
        });
    }
}