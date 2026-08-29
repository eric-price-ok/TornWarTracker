using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using TornWarTracker.Models;

namespace TornWarTracker.Services
{
    /// <summary>
    /// Appends every poll's snapshot of the enemy roster to a local SQLite
    /// database so you keep a history across the war. The live grid is
    /// driven entirely from in-memory data - this is history/audit only,
    /// there's no viewer UI for it yet (query history.db directly with any
    /// SQLite tool if you want to look back at it).
    /// </summary>
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TornWarTracker");
            Directory.CreateDirectory(dir);
            var dbPath = Path.Combine(dir, "history.db");
            _connectionString = $"Data Source={dbPath}";
            Initialize();
        }

        private void Initialize()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS MemberSnapshots (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    FactionId INTEGER NOT NULL,
                    PlayerId INTEGER NOT NULL,
                    Name TEXT NOT NULL,
                    Level INTEGER NOT NULL,
                    StatusState TEXT NOT NULL,
                    StatusUntil INTEGER NOT NULL,
                    LastActionStatus TEXT NOT NULL,
                    LastActionTimestamp INTEGER NOT NULL,
                    BsEstimate INTEGER NULL,
                    FairFight REAL NULL,
                    PolledAtUtc TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS IX_MemberSnapshots_Player
                    ON MemberSnapshots (FactionId, PlayerId, PolledAtUtc);
            ";
            cmd.ExecuteNonQuery();
        }

        public void SaveSnapshot(int factionId, IEnumerable<FactionMember> members, IReadOnlyDictionary<int, FfStatEstimate> estimates)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var transaction = conn.BeginTransaction();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO MemberSnapshots
                    (FactionId, PlayerId, Name, Level, StatusState, StatusUntil,
                     LastActionStatus, LastActionTimestamp, BsEstimate, FairFight, PolledAtUtc)
                VALUES
                    ($factionId, $playerId, $name, $level, $statusState, $statusUntil,
                     $lastActionStatus, $lastActionTimestamp, $bsEstimate, $fairFight, $polledAt);
            ";

            var pFactionId = cmd.CreateParameter(); pFactionId.ParameterName = "$factionId"; cmd.Parameters.Add(pFactionId);
            var pPlayerId = cmd.CreateParameter(); pPlayerId.ParameterName = "$playerId"; cmd.Parameters.Add(pPlayerId);
            var pName = cmd.CreateParameter(); pName.ParameterName = "$name"; cmd.Parameters.Add(pName);
            var pLevel = cmd.CreateParameter(); pLevel.ParameterName = "$level"; cmd.Parameters.Add(pLevel);
            var pStatusState = cmd.CreateParameter(); pStatusState.ParameterName = "$statusState"; cmd.Parameters.Add(pStatusState);
            var pStatusUntil = cmd.CreateParameter(); pStatusUntil.ParameterName = "$statusUntil"; cmd.Parameters.Add(pStatusUntil);
            var pLastActionStatus = cmd.CreateParameter(); pLastActionStatus.ParameterName = "$lastActionStatus"; cmd.Parameters.Add(pLastActionStatus);
            var pLastActionTimestamp = cmd.CreateParameter(); pLastActionTimestamp.ParameterName = "$lastActionTimestamp"; cmd.Parameters.Add(pLastActionTimestamp);
            var pBsEstimate = cmd.CreateParameter(); pBsEstimate.ParameterName = "$bsEstimate"; cmd.Parameters.Add(pBsEstimate);
            var pFairFight = cmd.CreateParameter(); pFairFight.ParameterName = "$fairFight"; cmd.Parameters.Add(pFairFight);
            var pPolledAt = cmd.CreateParameter(); pPolledAt.ParameterName = "$polledAt"; cmd.Parameters.Add(pPolledAt);

            var polledAt = DateTime.UtcNow.ToString("o");

            foreach (var member in members)
            {
                estimates.TryGetValue(member.Id, out var est);

                pFactionId.Value = factionId;
                pPlayerId.Value = member.Id;
                pName.Value = member.Name;
                pLevel.Value = member.Level;
                pStatusState.Value = member.Status.State;
                pStatusUntil.Value = member.Status.Until;
                pLastActionStatus.Value = member.LastAction.Status;
                pLastActionTimestamp.Value = member.LastAction.Timestamp;
                pBsEstimate.Value = (object?)est?.BsEstimate ?? DBNull.Value;
                pFairFight.Value = (object?)est?.FairFight ?? DBNull.Value;
                pPolledAt.Value = polledAt;

                cmd.ExecuteNonQuery();
            }

            transaction.Commit();
        }
    }
}
