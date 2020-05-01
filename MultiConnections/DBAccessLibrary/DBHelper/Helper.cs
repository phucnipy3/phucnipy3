﻿using DBAccessLibary.DBAccess;
using DBAccessLibary.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBAccessLibrary.DBHelper
{
    public static class Helper
    {
        
        public static async Task<StoredMatch> AddNewMatchAsync(
            StoredPlayer player1, 
            StoredPlayer player2, 
            bool isPlayer1PlayFirst, 
            bool blockTwoEndsRule, 
            int gameCount
            )
        {
            using (ModuleContext DB = new ModuleContext())
            {
                StoredMatch match = new StoredMatch()
                {
                    Player1 = player1,
                    Player2 = player2,
                    IsPlayer1PlayFirst = isPlayer1PlayFirst,
                    HasBlockTwoEndsRule = blockTwoEndsRule,
                    GameCount = gameCount,
                    DateTime = DateTime.Now
                };
                DB.Matches.Add(match);
                await DB.SaveChangesAsync();
                return match;
            }
        }
        public static StoredPlayer GetPlayerByName(string name)
        {
            using (ModuleContext DB = new ModuleContext())
            {
                return DB.Players.Where(x => x.Username == name).SingleOrDefault();
            }
        }
        public static async Task<StoredGame> AddNewGameToMatchAsync(StoredMatch match)
        {
            using (ModuleContext DB = new ModuleContext())
            {
                StoredGame game = new StoredGame();
                match.Games.Add(game);
                game.Order = match.Games.Count;
                await DB.SaveChangesAsync();
                return game;
            }
        }
        public static async Task AddMoveAsync(StoredGame game, string chessman, int coordinateX, int coordinateY)
        {
            using (ModuleContext DB = new ModuleContext())
            {
                Move move = new Move()
                {
                    Chessman = chessman,
                    CoordinateX = coordinateX,
                    CoordinateY = coordinateY
                };
                game.Moves.Add(move);
                move.Order = game.Moves.Count;
                await DB.SaveChangesAsync();
            }
        }
        public static async Task SetWinnerAsync(StoredPlayer winner, StoredGame game)
        {
            using (ModuleContext DB = new ModuleContext())
            {
                game.Winner = winner;
                await DB.SaveChangesAsync();
            }
        }

        public static async Task SwapPlayerAsync(StoredMatch storedMatch)
        {
            using (ModuleContext DB = new ModuleContext())
            {
                if (storedMatch.GameCount < 1)
                {
                    storedMatch.IsPlayer1PlayFirst = !storedMatch.IsPlayer1PlayFirst;
                    await DB.SaveChangesAsync();
                }
            }
        }
        
        public static bool Login(string infomation)
        {
            using (ModuleContext DB = new ModuleContext())
            {
                var players = DB.Players;
                return players.Where(x => infomation.Equals("[username]" + x.Username + "[password]" + x.Password + "[end]")).Count() > 0;
            }
        }
        public static async Task<bool> AddPlayerAsync(string username, string password)
        {
            using (ModuleContext DB = new ModuleContext())
            {
                if (DB.Players.Where(x => x.Username == username).Count() > 0)
                    return false;
                StoredPlayer player = new StoredPlayer() { Username = username, Password = password };
                DB.Players.Add(player);
                await DB.SaveChangesAsync();
                return true;
            }
        }

        public static async Task<StoredMatch> GetMatchAsync(int id)
        {
            using (ModuleContext DB = new ModuleContext())
            {
                return await DB.Matches.Include("Player1").Include("Player2").Include("Games.Moves").Include("Games.Winner").Where(x => x.Id == id).SingleOrDefaultAsync();
            }
        }

      
    }
}
