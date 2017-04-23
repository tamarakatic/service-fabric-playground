using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Game.Interfaces;
using Microsoft.ServiceFabric.Data;

namespace Game
{
    /// <remarks>
    /// This class represents an actor.
    /// Every ActorID maps to an instance of this class.
    /// The StatePersistence attribute determines persistence and replication of actor state:
    ///  - Persisted: State is written to disk and replicated.
    ///  - Volatile: State is kept in memory only and replicated.
    ///  - None: State is kept in memory only and not replicated.
    /// </remarks>
    [StatePersistence(StatePersistence.Persisted)]
    internal class Game : Actor, IGame
    {
        /// <summary>
        /// Initializes a new instance of Game
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        public Game(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        /// <summary>
        /// This method is called whenever an actor is activated.
        /// An actor is activated the first time any of its methods are invoked.
        /// </summary>
        protected override async Task OnActivateAsync()
        {
            ConditionalValue<ActorState> state = await this.StateManager.TryGetStateAsync<ActorState>("MyActorState");
            if (!state.HasValue)
            {
                var actorState = new ActorState()
                {
                    Board = new int[9],
                    NextPlayerIndex = 0,
                    NumberOfMoves = 0,
                    Players = new List<Tuple<long, string>>(),
                    Winner = ""
                };
                await this.StateManager.SetStateAsync<ActorState>("MyActorState", actorState);
            }
        }

        public async Task<bool> JoinGameAsync(long playerId, string playerName)
        {
            var actorState = await GetAstorState();

            if (actorState.Players.Count >= 2 ||
               actorState.Players.FirstOrDefault(p => p.Item2 == playerName) != null)
                return await Task.FromResult<bool>(false);

            actorState.Players.Add(new Tuple<long, string>(playerId, playerName));
            await SetActorState(actorState);
            return await Task.FromResult<bool>(true);
        }

        public async Task<ActorState> GetAstorState()
        {
            ConditionalValue<ActorState> state = await this.StateManager.TryGetStateAsync<ActorState>("MyActorState");
            return await Task.FromResult<ActorState>(state.Value);
        }

        public async Task SetActorState(ActorState state)
        {
            await this.StateManager.AddOrUpdateStateAsync<ActorState>("MyActorState", state,
                (key, value) => state);
        }

        [ReadOnly(true)]
        public async Task<int[]> GetGameBoardAsync()
        {
            var actorState = await GetAstorState();
            return await Task.FromResult<int[]>(actorState.Board);
        }

        [ReadOnly(true)]
        public async Task<string> GetWinnerAsync()
        {
            var actorState = await GetAstorState();
            return await Task.FromResult<string>(actorState.Winner);
        }

        public async Task<bool> MakeMoveAsync(long playerId, int x, int y)
        {
            var actorState = await GetAstorState();
            if (x < 0 || x > 2 || y < 0 || y > 2
                || actorState.Players.Count != 2
                || actorState.NumberOfMoves >= 9
                || actorState.Winner != "")
                return await Task.FromResult<bool>(false);

            int index = actorState.Players.FindIndex(p => p.Item1 == playerId);
            if (index == actorState.NextPlayerIndex)
            {
                if (actorState.Board[y * 3 + x] == 0)
                {
                    int piece = index * 2 - 1;
                    actorState.Board[y * 3 + x] = piece;
                    actorState.NumberOfMoves++;

                    if (await HasWon(piece * 3))
                        actorState.Winner = actorState.Players[index].Item2 + " (" +
                                            (piece == -1 ? "X" : "0") + ")";
                    else if (actorState.Winner == "" && actorState.NumberOfMoves >= 9)
                        actorState.Winner = "TIE";

                    actorState.NextPlayerIndex = (actorState.NextPlayerIndex + 1) % 2;
                    return await Task.FromResult<bool>(true);
                }
                return await Task.FromResult<bool>(false);
            }
            return await Task.FromResult<bool>(false);
        }

        private async Task<bool> HasWon(int sum)
        {
            var actorState = await GetAstorState();
            var result = actorState.Board[0] + actorState.Board[1] + actorState.Board[2] == sum
                   || actorState.Board[0] + actorState.Board[1] + actorState.Board[2] == sum
                   || actorState.Board[3] + actorState.Board[4] + actorState.Board[5] == sum
                   || actorState.Board[6] + actorState.Board[7] + actorState.Board[8] == sum
                   || actorState.Board[0] + actorState.Board[3] + actorState.Board[6] == sum
                   || actorState.Board[1] + actorState.Board[4] + actorState.Board[7] == sum
                   || actorState.Board[2] + actorState.Board[5] + actorState.Board[8] == sum
                   || actorState.Board[0] + actorState.Board[4] + actorState.Board[8] == sum
                   || actorState.Board[2] + actorState.Board[4] + actorState.Board[6] == sum;
            return await Task.FromResult<bool>(result);
        }
    }
}
