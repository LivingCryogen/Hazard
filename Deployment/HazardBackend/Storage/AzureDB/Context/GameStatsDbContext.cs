using HazardBackend.Storage.AzureDB.Entities;
using Microsoft.EntityFrameworkCore;

namespace HazardBackend.Storage.AzureDB.Context;

public class GameStatsDbContext(DbContextOptions<GameStatsDbContext> options) : DbContext(options)
{
    public DbSet<GameSessionEntity> GameSessions { get; set; }
    public DbSet<ClaimActionEntity> ClaimActions { get; set; }
    public DbSet<AttackActionEntity> AttackActions { get; set; }
    public DbSet<MoveActionEntity> MoveActions { get; set; }
    public DbSet<TradeActionEntity> TradeActions { get; set; }
    public DbSet<AcquiredContinentEventEntity> AcquiredContinents { get; set; }
    public DbSet<PlayerStatsEntity> PlayerStats { get; set; }
    public DbSet<PlayerIdentityEntity> PlayerIdentities { get; set; }
    public DbSet<GameSessionPlayerEntity> GameSessionPlayers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Primary Keys
        modelBuilder.Entity<GameSessionEntity>()
            .HasKey(entity => entity.GameId);
            
        modelBuilder.Entity<PlayerIdentityEntity>()
            .HasKey(entity => new { entity.Name, entity.InstallId });

        modelBuilder.Entity<GameSessionPlayerEntity>()
            .HasKey(entity => new { entity.GameId, entity.PlayerName, entity.InstallId });

        modelBuilder.Entity<PlayerStatsEntity>()
            .HasKey(entity => new { entity.Name, entity.InstallId });

        modelBuilder.Entity<ClaimActionEntity>()
            .HasKey(entity => new { entity.GameId, entity.ActionId });

        modelBuilder.Entity<AttackActionEntity>()
            .HasKey(entity => new { entity.GameId, entity.ActionId });

        modelBuilder.Entity<MoveActionEntity>()
            .HasKey(entity => new { entity.GameId, entity.ActionId });

        modelBuilder.Entity<TradeActionEntity>()
            .HasKey(entity => new { entity.GameId, entity.ActionId });

        modelBuilder.Entity<AcquiredContinentEventEntity>()
            .HasKey(entity => new { entity.GameId, entity.FromActionId });

        // Foreign Keys and Relationships

        modelBuilder.Entity<PlayerStatsEntity>()
            .HasOne<PlayerIdentityEntity>()
            .WithOne(pi => pi.PlayerStats)
            .HasForeignKey<PlayerStatsEntity>(e => new { e.Name, e.InstallId })
            .HasPrincipalKey<PlayerIdentityEntity>(e => new { e.Name, e.InstallId });

        // GameSessionPlayerEntity Junction table relationships
        modelBuilder.Entity<GameSessionPlayerEntity>() 
            .HasOne<GameSessionEntity>()
            .WithMany(game => game.GameSessionPlayers)
            .HasForeignKey(gsp => gsp.GameId)
            .OnDelete(DeleteBehavior.Cascade); // Cascade delete GameSessionPlayerEntities when a GameSession is deleted
       
        modelBuilder.Entity<GameSessionPlayerEntity>()
            .HasOne<PlayerIdentityEntity>()
            .WithMany()
            .HasForeignKey(plyr => new { plyr.PlayerName, plyr.InstallId })
            .HasPrincipalKey(pi => new { pi.Name, pi.InstallId })
            .OnDelete(DeleteBehavior.Restrict);

        // Action relationships
        modelBuilder.Entity<ClaimActionEntity>()
            .HasOne(claim => claim.GameSession)
            .WithMany(game => game.ClaimActions)
            .OnDelete(DeleteBehavior.Cascade); // Cascade delete actions when a GameSession is deleted

        modelBuilder.Entity<ClaimActionEntity>()
            .HasOne<PlayerIdentityEntity>()
            .WithMany() // No navigation property in PlayerIdentityEntity, doesn't track back to actions
            .HasForeignKey(c => new { c.PlayerName, c.InstallID })
            .HasPrincipalKey(p => new { p.Name, p.InstallId })
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes

        modelBuilder.Entity<AttackActionEntity>()
            .HasOne(attack => attack.GameSession)
            .WithMany(game => game.AttackActions)
            .OnDelete(DeleteBehavior.Cascade); // Cascade delete actions when a GameSession is deleted

        modelBuilder.Entity<AttackActionEntity>()
            .HasOne<PlayerIdentityEntity>()
            .WithMany() // No navigation property in PlayerIdentityEntity, doesn't track back to actions
            .HasForeignKey(a => new { a.PlayerName, a.InstallID })
            .HasPrincipalKey(p => new { p.Name, p.InstallId })
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes

        modelBuilder.Entity<AttackActionEntity>()
            .HasOne<PlayerIdentityEntity>()
            .WithMany() // No navigation property in PlayerIdentityEntity, doesn't track back to actions
            .HasForeignKey(a => new { a.DefenderName, a.InstallID })
            .HasPrincipalKey(p => new { p.Name, p.InstallId })
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes

        modelBuilder.Entity<MoveActionEntity>()
            .HasOne(move => move.GameSession)
            .WithMany(game => game.MoveActions)
            .OnDelete(DeleteBehavior.Cascade); // Cascade delete actions when a GameSession is deleted

        modelBuilder.Entity<MoveActionEntity>()
            .HasOne<PlayerIdentityEntity>()
            .WithMany() // No navigation property in PlayerIdentityEntity, doesn't track back to actions
            .HasForeignKey(m => new { m.PlayerName, m.InstallID })
            .HasPrincipalKey(m => new { m.Name, m.InstallId })
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes

        modelBuilder.Entity<TradeActionEntity>()
            .HasOne(trade => trade.GameSession)
            .WithMany(game => game.TradeActions)
            .OnDelete(DeleteBehavior.Cascade); // Cascade delete actions when a GameSession is deleted

        modelBuilder.Entity<TradeActionEntity>()
            .HasOne<PlayerIdentityEntity>()
            .WithMany() // No navigation property in PlayerIdentityEntity, doesn't track back to actions
            .HasForeignKey(t => new { t.PlayerName, t.InstallID })
            .HasPrincipalKey(t => new { t.Name, t.InstallId })
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes

        // Event relationships
        modelBuilder.Entity<AcquiredContinentEventEntity>()
            .HasOne(acquired => acquired.GameSession)
            .WithMany(game => game.AcquiredContinents)
            .OnDelete(DeleteBehavior.Cascade); // Cascade delete events when a GameSession is deleted

        modelBuilder.Entity<AcquiredContinentEventEntity>()
            .HasOne<PlayerIdentityEntity>()
            .WithMany() // No navigation property in PlayerIdentityEntity, doesn't track back
            .HasForeignKey(a => new { a.NewOwner, a.InstallId })
            .HasPrincipalKey(p => new { p.Name, p.InstallId })
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes

        modelBuilder.Entity<AcquiredContinentEventEntity>()
            .HasOne<PlayerIdentityEntity>()
            .WithMany() // No navigation property in PlayerIdentityEntity, doesn't track back
            .HasForeignKey(a => new { a.PrevOwner, a.InstallId })
            .HasPrincipalKey(p => new { p.Name, p.InstallId })
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes
    }
}
