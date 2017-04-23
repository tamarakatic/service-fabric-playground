using System;
using System.Threading.Tasks;
using Game.Interfaces;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Player.Interfaces;

namespace TestClient
{
    class Program
    {
        public static void Main(string[] args)
        {
            var firstPlayer = ActorProxy.Create<IPlayer>(ActorId.CreateRandom(), "fabric:/ActorTicTacToeApplication");
            var secondPlayer = ActorProxy.Create<IPlayer>(ActorId.CreateRandom(), "fabric:/ActorTicTacToeApplication");

            var gameId = ActorId.CreateRandom();
            var game = ActorProxy.Create<IGame>(gameId, "fabric:/ActorTicTacToeApplication");

            var firstPlayerResult = firstPlayer.JoinGameAsync(gameId, "Player I");
            var secondPlayerResult = secondPlayer.JoinGameAsync(gameId, "Player II");

            if (!firstPlayerResult.Result || !secondPlayerResult.Result)
                Console.WriteLine("Failed to join game!");

            Task.Run(() => { MakeMove(firstPlayer, game, gameId); });
            Task.Run(() => { MakeMove(secondPlayer, game, gameId); });

            var gameTask = Task.Run(() =>
            {
                string winner = "";
                while (winner == "")
                {
                    var board = game.GetGameBoardAsync().Result;
                    PrintBoard(board);
                    winner = game.GetWinnerAsync().Result;
                    Task.Delay(1000).Wait();
                }

                Console.WriteLine("\n ***** Winner is: " + winner + " *****");
            });

            gameTask.Wait();
            Console.Read();
        }

        private static void PrintBoard(int[] board)
        {
            Console.Clear();

            for (int i = 0; i < board.Length; i++)
            {
                if (board[i] == -1)
                    Console.Write(" X ");
                else if (board[i] == 1)
                    Console.Write(" 0 ");
                else
                    Console.Write(" . ");

                if ((i + 1) % 3 == 0)
                    Console.WriteLine();
            }
        }

        private static async void MakeMove(IPlayer player, IGame game, ActorId gameId)
        {
            Random rand = new Random();
            while (true)
            {
                await player.MakeMoveAsync(gameId, rand.Next(0, 3), rand.Next(0, 3));
                await Task.Delay(rand.Next(500, 1000));
            }
        }
    }
}
