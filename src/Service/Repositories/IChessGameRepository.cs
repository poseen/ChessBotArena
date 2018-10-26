﻿using BoardGame.Service.Models.Api.ChessGamesControllerModels;
using BoardGame.Service.Models.Data;
using System;
using System.Collections.Generic;

namespace BoardGame.Service.Repositories
{
    /// <summary>
    /// Interface of the chess game repository.
    /// </summary>
    public interface IChessGameRepository
    {
        /// <summary>
        /// Gets the list of chess games according to the given predicate.
        /// </summary>
        /// <param name="participantPlayerName">Username of the participant (either sides) to filter for.</param>
        /// <param name="predicate">Predicate for additional filtering if needed.</param>
        /// <returns>List of chess games.</returns>
        IReadOnlyList<ChessGame> Get(string participantPlayerName, Func<DbChessGame, bool> predicate = null);

        /// <summary>
        /// Gets the list of chess game details according to the given predicate.
        /// </summary>
        /// <param name="participantPlayerName">Username of the participant (either sides) to filter for.</param>
        /// <param name="predicate">Predicate for additional filtering if needed.</param>
        /// <returns>List of detailed chess games.</returns>
        IReadOnlyList<ChessGameDetails> GetDetails(string participantPlayerName, Func<DbChessGame, bool> predicate = null);

        /// <summary>
        /// Validates and saves a new game party according to the supplied challenge request.
        /// </summary>
        /// <param name="challengeRequest">The request coming from the API.</param>
        /// <returns>Returns the result of the validation and the operation.</returns>
        ChessGameRepositoryAddResult Add(Challenge challengeRequest);
    }
}