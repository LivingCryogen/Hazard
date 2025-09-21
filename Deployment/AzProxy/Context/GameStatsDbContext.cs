using AzProxy.Entities;
using Microsoft.EntityFrameworkCore;

namespace AzProxy.Context;

public class GameStatsDbContext(DbContextOptions<GameStatsDbContext> options) : DbContext(options)
{
    public DbSet<GameSessionEntity> GameSessions { get; set; }
    public DbSet<GamePlayerEntity> GamePlayers { get; set; }
    public DbSet<AttackActionEntity> AttackActions { get; set; }
    public DbSet<MoveActionEntity> MoveActions { get; set; }
    public DbSet<TradeActionEntity> TradeActions { get; set; }
    
    public DbSet<PlayerStatsEntity> PlayerStats { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<GameSessionEntity>()
            .HasKey(entity => new { entity.GameId, entity.InstallId });

        modelBuilder.Entity<GamePlayerEntity>()
            .HasKey(entity => new { entity.GameId, entity.PlayerNumber });

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

        modelBuilder.Entity<MoveActionEntity>()
            .HasOne(move => move.GameSession)
            .WithMany(game => game.MoveActions);

        modelBuilder.Entity<TradeActionEntity>()
            .HasOne(trade => trade.GameSession)
            .WithMany(game => game.TradeActions);

        modelBuilder.Entity<GamePlayerEntity>()
            .HasOne(player => player.GameSession)
            .WithMany(game => game.Players);
    }
}
