﻿using BoardGame.Algorithms.Abstractions.Interfaces;
using BoardGame.Game.Chess;
using BoardGame.Game.Chess.Moves;

namespace BoardGame.Clients.BotClient
{
    internal class MoveApplier : IApplier<ChessRepresentation, BaseMove>
    {
        private readonly ChessMechanism _mechanism;

        public MoveApplier(ChessMechanism mechanism)
        {
            _mechanism = mechanism;
        }

        public ChessRepresentation Apply(ChessRepresentation state, BaseMove move)
        {
            return _mechanism.ApplyMove(state, move);
        }
    }
}
