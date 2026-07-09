IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [GameSessions] (
    [GameId] uniqueidentifier NOT NULL,
    [InstallId] uniqueidentifier NOT NULL,
    [IsDemo] bit NOT NULL,
    [Version] int NOT NULL,
    [StartTime] datetime2 NOT NULL,
    [EndTime] datetime2 NULL,
    [Winner] int NULL,
    CONSTRAINT [PK_GameSessions] PRIMARY KEY ([GameId])
);

CREATE TABLE [PlayerStats] (
    [InstallId] uniqueidentifier NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [IsDemo] bit NOT NULL,
    [GamesStarted] int NOT NULL,
    [GamesCompleted] int NOT NULL,
    [GamesWon] int NOT NULL,
    [FirstGameStarted] datetime2 NOT NULL,
    [FirstGameCompleted] datetime2 NULL,
    [LastGameStarted] datetime2 NOT NULL,
    [LastGameCompleted] datetime2 NULL,
    [TotalGamesDuration] time NOT NULL,
    [AttacksWon] int NOT NULL,
    [AttacksLost] int NOT NULL,
    [AttacksTied] int NOT NULL,
    [Conquests] int NOT NULL,
    [Retreats] int NOT NULL,
    [ForcedRetreats] int NOT NULL,
    [AttackDiceRolled] int NOT NULL,
    [DefenseDiceRolled] int NOT NULL,
    [Moves] int NOT NULL,
    [MaxAdvances] int NOT NULL,
    [TradeIns] int NOT NULL,
    [TotalOccupationBonus] int NOT NULL,
    CONSTRAINT [PK_PlayerStats] PRIMARY KEY ([Name], [InstallId])
);

CREATE TABLE [AttackActions] (
    [GameId] uniqueidentifier NOT NULL,
    [ActionId] int NOT NULL,
    [PlayerName] nvarchar(450) NOT NULL,
    [InstallID] uniqueidentifier NOT NULL,
    [IsDemo] bit NOT NULL,
    [SourceTerritory] nvarchar(max) NOT NULL,
    [TargetTerritory] nvarchar(max) NOT NULL,
    [DefenderName] nvarchar(450) NOT NULL,
    [AttackerInitialArmies] int NOT NULL,
    [DefenderInitialArmies] int NOT NULL,
    [AttackerDice] int NOT NULL,
    [DefenderDice] int NOT NULL,
    [AttackerLoss] int NOT NULL,
    [DefenderLoss] int NOT NULL,
    [Retreated] bit NOT NULL,
    [Conquered] bit NOT NULL,
    CONSTRAINT [PK_AttackActions] PRIMARY KEY ([GameId], [ActionId]),
    CONSTRAINT [FK_AttackActions_GameSessions_GameId] FOREIGN KEY ([GameId]) REFERENCES [GameSessions] ([GameId]) ON DELETE CASCADE,
    CONSTRAINT [FK_AttackActions_PlayerStats_DefenderName_InstallID] FOREIGN KEY ([DefenderName], [InstallID]) REFERENCES [PlayerStats] ([Name], [InstallId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AttackActions_PlayerStats_PlayerName_InstallID] FOREIGN KEY ([PlayerName], [InstallID]) REFERENCES [PlayerStats] ([Name], [InstallId]) ON DELETE NO ACTION
);

CREATE TABLE [MoveActions] (
    [GameId] uniqueidentifier NOT NULL,
    [ActionId] int NOT NULL,
    [PlayerName] nvarchar(450) NOT NULL,
    [InstallID] uniqueidentifier NOT NULL,
    [IsDemo] bit NOT NULL,
    [SourceTerritory] nvarchar(max) NOT NULL,
    [TargetTerritory] nvarchar(max) NOT NULL,
    [MaxAdvanced] bit NOT NULL,
    CONSTRAINT [PK_MoveActions] PRIMARY KEY ([GameId], [ActionId]),
    CONSTRAINT [FK_MoveActions_GameSessions_GameId] FOREIGN KEY ([GameId]) REFERENCES [GameSessions] ([GameId]) ON DELETE CASCADE,
    CONSTRAINT [FK_MoveActions_PlayerStats_PlayerName_InstallID] FOREIGN KEY ([PlayerName], [InstallID]) REFERENCES [PlayerStats] ([Name], [InstallId]) ON DELETE NO ACTION
);

CREATE TABLE [TradeActions] (
    [GameId] uniqueidentifier NOT NULL,
    [ActionId] int NOT NULL,
    [PlayerName] nvarchar(450) NOT NULL,
    [InstallID] uniqueidentifier NOT NULL,
    [IsDemo] bit NOT NULL,
    [CardTargets] nvarchar(max) NOT NULL,
    [TradeValue] int NOT NULL,
    [OccupiedBonus] int NOT NULL,
    CONSTRAINT [PK_TradeActions] PRIMARY KEY ([GameId], [ActionId]),
    CONSTRAINT [FK_TradeActions_GameSessions_GameId] FOREIGN KEY ([GameId]) REFERENCES [GameSessions] ([GameId]) ON DELETE CASCADE,
    CONSTRAINT [FK_TradeActions_PlayerStats_PlayerName_InstallID] FOREIGN KEY ([PlayerName], [InstallID]) REFERENCES [PlayerStats] ([Name], [InstallId]) ON DELETE NO ACTION
);

CREATE INDEX [IX_AttackActions_DefenderName_InstallID] ON [AttackActions] ([DefenderName], [InstallID]);

CREATE INDEX [IX_AttackActions_PlayerName_InstallID] ON [AttackActions] ([PlayerName], [InstallID]);

CREATE INDEX [IX_MoveActions_PlayerName_InstallID] ON [MoveActions] ([PlayerName], [InstallID]);

CREATE INDEX [IX_TradeActions_PlayerName_InstallID] ON [TradeActions] ([PlayerName], [InstallID]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251012171154_InitialCreate', N'9.0.9');

COMMIT;
GO

