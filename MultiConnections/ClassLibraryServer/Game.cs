﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.IO;
using DBAccessLibary.Models;
using DBAccessLibrary.DBHelper;
using System.Threading;

namespace ClassLibraryServer
{
    public class Game
    {
        public enum Chessman { X = 1, O = 2, Empty = 0 }
        private const int BOARD_SIZE = 20;
        private const int BYTES_SIZE = 1024;
        private const int MAX_CHESSMAN_COUNT = 5;
        private Chessman[,] matrix;
        private Player firstPlayer;
        private Player secondPlayer;
        private Player winner;
        private bool gameEnded;
        private string moveString;
        private Chessman chessman;
        private int moveCount;
        private StoredGame storedGame;
        public Player Winner
        {
            get { return winner; }
        }
        private Game(Player firstPlayer, Player secondPlayer, StoredMatch match)
        {

            this.firstPlayer = firstPlayer;
            this.secondPlayer = secondPlayer;
            gameEnded = false;
            moveCount = 1;
            InitBoard();
        }

        public static async Task<Game> CreateNewGameAsync(Player firstPlayer, Player secondPlayer, StoredMatch match)
        {
            Game game = new Game(firstPlayer, secondPlayer, match);
            game.storedGame = await Helper.AddNewGameToMatchAsync(match);
            return game;
        }
        private void InitBoard()
        {
            matrix = new Chessman[BOARD_SIZE, BOARD_SIZE];
            for (int i = 0; i < BOARD_SIZE; i++)
                for (int j = 0; j < BOARD_SIZE; j++)
                    matrix[i, j] = Chessman.Empty;
        }
        public async Task StartAsync ()
        {
            SendStartMessageToFirstPlayer();
            SendStartMessageToSecondPlayer();
            while (true)
            {
                chessman = Chessman.O;
                await ProcessingDataFromAsync(firstPlayer);
                if (gameEnded)
                {
                    winner = firstPlayer;
                    await Helper.SetWinnerAsync(Helper.GetPlayerByName(firstPlayer.Name), storedGame);
                    return;
                }
                TrySendData(secondPlayer, moveString + "[end]");
                chessman = Chessman.X;
                await ProcessingDataFromAsync(secondPlayer);
                if (gameEnded)
                {
                    winner = secondPlayer;
                    await Helper.SetWinnerAsync(Helper.GetPlayerByName(secondPlayer.Name), storedGame);
                    return;
                }
                TrySendData(firstPlayer, moveString + "[end]");
            }
        }

        private void SendStartMessageToFirstPlayer()
        {
            TrySendData(firstPlayer, "playfirst[end]");
        }
        private void SendStartMessageToSecondPlayer()
        {
            TrySendData(secondPlayer, "playsecond[end]");
        }

        private async Task ProcessingDataFromAsync(Player player)
        {
            moveString = TryGetData(player);
            if (StrValid())
            { 
                await MakeMoveAsync();
                if (GameOver())
                {
                    gameEnded = true;
                }
            }
            else
            {
                MessageBox.Show("str not valid");
            }
        }

        public string TryGetData(Player player)
        {
            while (true)
            {
                try
                {
                    return GetData(player.Client);
                }
                catch(Exception e)
                {
                    Thread.Sleep(2000);
                    continue;
                }
            }  
        }
        public string GetData(TcpClient player)
        {

            byte[] buffer = new byte[BYTES_SIZE];
            NetworkStream stream = player.GetStream();
            stream.ReadTimeout = 10000;
            stream.Read(buffer, 0, buffer.Length);
            string data = Encoding.ASCII.GetString(buffer);
            return data.Substring(0,data.LastIndexOf("[end]"));
        }
        public bool StrValid()
        {
            string[] move = moveString.Split(',');
            if (move.Length >= 2)
            {
                try
                {
                    int row = int.Parse(move[0]);
                    int col = int.Parse(move[1]);
                    if (isInsideBoard(row, col))
                        if (matrix[row, col] == Chessman.Empty)
                            return true;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }
        public async Task MakeMoveAsync()
        {
            string[] move = moveString.Split(',');
            int row = int.Parse(move[0]);
            int col = int.Parse(move[1]);
            matrix[row, col] = chessman;
            string saveValue = chessman.ToString() + moveCount.ToString();
            moveCount++;
            await Helper.AddMoveAsync(storedGame, chessman == Chessman.X ? "X" : "O", col, row);
        }

        public bool GameOver()

        {

            int[] X = new int[] { 5, 4, 3, 2, 1, 0, -1, -2, -3, -4, -5 };
            int[] Y = new int[] { -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5 };
            int[] Z = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            string[] move = moveString.Split(',');
            int row = int.Parse(move[0]);
            int col = int.Parse(move[1]);
            if (CanWin(X, Y, matrix, row, col) ||
                CanWin(X, Z, matrix, row, col) ||
                CanWin(Z, X, matrix, row, col) ||
                CanWin(X, X, matrix, row, col))
                return true;
            return false;
        }
        public bool CanWin(int[] X, int[] Y, Chessman[,] banCo, int i, int j)
        {
            int ContinueChessmanCount = 0;
            for (int k = 1; k < X.Length - 1; k++)
            {
                int rowNum = i + X[k], colNum = j + Y[k];
                if (isInsideBoard(rowNum, colNum))
                {

                    if (banCo[rowNum, colNum] == chessman)
                    {
                        ContinueChessmanCount++;
                        if (ContinueChessmanCount == MAX_CHESSMAN_COUNT)
                        {
                            if (isBlocked(X, Y, banCo, chessman, i, j, k))
                                return false;
                            else
                                return true;
                        }
                    }
                    else
                        ContinueChessmanCount = 0;
                }
            }
            return false;
        }
        public bool isBlocked(int[] X, int[] Y, Chessman[,] matrix, Chessman chessman, int i, int j, int k)
        {
            int upsideX = i + X[k + 1], upsideY = j + Y[k + 1];
            int downsideX = i + X[k - MAX_CHESSMAN_COUNT], downsideY = j + Y[k - MAX_CHESSMAN_COUNT];

            if (isInsideBoard(upsideX, upsideY) && isInsideBoard(downsideX, downsideY))
                if ((matrix[upsideX, upsideY] != chessman && matrix[upsideX, upsideY] != Chessman.Empty) &&
                    (matrix[downsideX, downsideY] != chessman && matrix[downsideX, downsideY] != Chessman.Empty))
                    return true;
            return false;
        }
        public bool isInsideBoard(int row, int col)
        {
            return row >= 0 && row < BOARD_SIZE && col >= 0 && col < BOARD_SIZE;
        }

        public void TrySendData(Player player, string msg)
        {
            while(true)
            {
                try
                {
                    SendData(player, msg);
                    break;
                }
                catch
                {
                    Thread.Sleep(2000);
                    continue;
                }
            }
        }
        public void SendData(Player player, string msg)
        {
            byte[] buffer = new byte[BYTES_SIZE];
            buffer = Encoding.ASCII.GetBytes(msg);
            NetworkStream stream = player.Client.GetStream();
            stream.WriteTimeout = 10000;
            stream.Write(buffer, 0, buffer.Length);
        }
    }
}
