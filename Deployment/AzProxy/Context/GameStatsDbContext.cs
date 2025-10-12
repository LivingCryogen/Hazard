using AzProxy.Entities;
using Microsoft.EntityFrameworkCore;

namespace AzProxy.Context;

public class GameStatsDbContext(DbContextOptions<GameStatsDbContext> options) : DbContext(options)
{
    public DbSet<GameSessionEntity> GameSessions { get; set; }
    public DbSet<AttackActionEntity> AttackActions { get; set; }
    public DbSet<MoveActionEntity> MoveActions { get; set; }
    public DbSet<TradeActionEntity> TradeActions { get; set; }
    
    public DbSet<PlayerStatsEntity> PlayerStats { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<GameSessionEntity>()
            .HasKey(entity => entity.GameId);

        modelBuilder.Entity<PlayerStatsEntity>()
            .HasKey(entity => new { entity.Name, entity.InstallId });

        modelBuilder.Entity<AttackActionEntity>()
            .HasKey(entity => new { entity.GameId, entity.ActionId });

        modelBuilder.Entity<MoveActionEntity>()
            .HasKey(entity => new { entity.GameId, entity.ActionId });

        modelBuilder.Entity<TradeActionEntity>()
            .HasKey(entity => new { entity.GameId, entity.ActionId });

        modelBuilder.Entity<AttackActionEntity>()
            .HasOne(attack => attack.GameSession)
            .WithMany(game => game.AttackActions);

        modelBuilder.Entity<AttackActionEntity>()
            .HasOne<PlayerStatsEntity>()
            .WithMany() // No navigation property in PlayerStatsEntity, doesn't track back to actions
            .HasForeignKey(a => new { a.PlayerName, a.InstallID })
            .HasPrincipalKey(p => new { p.Name, p.InstallId })
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes

        modelBuilder.Entity<AttackActionEntity>()
            .HasOne<PlayerStatsEntity>()
            .WithMany() // No navigation property in PlayerStatsEntity, doesn't track back to actions
            .HasForeignKey(a => new { a.DefenderName, a.InstallID })
            .HasPrincipalKey(p => new { p.Name, p.InstallId })
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes

        modelBuilder.Entity<MoveActionEntity>()
            .HasOne(move => move.GameSession)
            .WithMany(game => game.MoveActions);

        modelBuilder.Entity<MoveActionEntity>()
            .HasOne<PlayerStatsEntity>()
            .WithMany() // No navigation property in PlayerStatsEntity, doesn't track back to actions
            .HasForeignKey(m => new { m.PlayerName, m.InstallID })
            .HasPrincipalKey(m => new { m.Name, m.InstallId })
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes

        modelBuilder.Entity<TradeActionEntity>()
            .HasOne(trade => trade.GameSession)
            .WithMany(game => game.TradeActions);

        modelBuilder.Entity<TradeActionEntity>()
            .HasOne<PlayerStatsEntity>()
            .WithMany() // No navigation property in PlayerStatsEntity, doesn't track back to actions
            .HasForeignKey(t => new { t.PlayerName, t.InstallID })
            .HasPrincipalKey(t => new { t.Name, t.InstallId })
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes
    }
}
