﻿using System;
using BoardGame.Game.Chess;

namespace BoardGame.Model.Api.ChessGamesControllerModels
{
    /// <summary>
    /// Interface to a chess game item in the chess games controller.
    /// </summary>
    public class ChessGame
    {
        /// <summary>
        /// Gets or sets the ID of the game.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the game.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the user who have initiated the game. (Challenged someone.)
        /// </summary>
        public ChessGamePlayerDto InitiatedBy { get; set; }

        /// <summary>
        /// Gets or sets the user who was challenged by the game initiator.
        /// </summary>
        public ChessGamePlayerDto Opponent { get; set; }

        /// <summary>
        /// Gets or sets the white player.
        /// </summary>
        public ChessGamePlayerDto WhitePlayer { get; set; }

        /// <summary>
        /// Gets or sets the black player.
        /// </summary>
        public ChessGamePlayerDto BlackPlayer { get; set; }

        /// <summary>
        /// Gets or sets the date the challenge was sent.
        /// </summary>
        public DateTime ChallengeDate { get; set; }

        /// <summary>
        /// Gets or sets the date of the last move. Initially set to the challenge date.
        /// </summary>
        public DateTime LastMoveDate { get; set; }

        /// <summary>
        /// Gets or sets the outcome of the match.
        /// </summary>
        public GameState Outcome { get; set; }
    }
}
