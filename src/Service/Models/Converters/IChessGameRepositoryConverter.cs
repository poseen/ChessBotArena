﻿using BoardGame.Service.Models.Api.ChessGamesControllerModels;
using BoardGame.Service.Models.Data;

namespace BoardGame.Service.Models.Converters
{
    /// <summary>
    /// Interface of the DB - API model converters used by the chess game repository.
    /// </summary>
    public interface IChessGameRepositoryConverter
    {
        /// <summary>
        /// Converts the DB side object to the API target type.
        /// </summary>
        /// <param name="source">Source object.</param>
        /// <returns>Null if the source is null. Otherwise the converted version.</returns>
        ChessGame ConvertToChessGame(DbChessGame source);

        /// <summary>
        /// Converts the DB side chess game object to the API side detailed object model.
        /// </summary>
        /// <param name="source">DB side source object.</param>
        /// <returns>API side detailed object.</returns>
        ChessGameDetails ConvertToChessGameDetails(DbChessGame source);
    }
}