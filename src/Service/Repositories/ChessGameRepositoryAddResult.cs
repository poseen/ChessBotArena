﻿using BoardGame.Service.Models.Api.ChessGamesControllerModels;

namespace BoardGame.Service.Repositories
{
    /// <summary>
    /// The result with additional result information.
    /// </summary>
    public class ChessGameRepositoryAddResult
    {
        /// <summary>
        /// The result of the addition.
        /// </summary>
        public ChallengeRequestResults RequestResult { get; set; }

        /// <summary>
        /// The newly created entity if the result was ok.
        /// </summary>
        public ChessGame NewlyCreatedGame { get; set; }
    }
}
