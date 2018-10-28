﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using BoardGame.Service.Data;
using BoardGame.Service.Models.Api.ChessGamesControllerModels;
using BoardGame.Service.Models.Converters;
using BoardGame.Service.Models.Data;
using BoardGame.Service.Models.Repositories.ChessGameRepository;
using BoardGame.Service.Extensions;
using Game.Chess;

namespace BoardGame.Service.Repositories
{
    /// <summary>
    /// Implementation of the chess game repository.
    /// </summary>
    public class ChessGameRepository : IChessGameRepository
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IChessGameRepositoryConverter _chessGameConverter;

        /// <summary>
        /// Initializes a new instance of the chess game repository.
        /// </summary>
        /// <param name="dbContext">The database context to be used to query the data.</param>
        /// <param name="chessGameConveter">Converter which transforms the inner database-near model to API model.</param>
        public ChessGameRepository(ApplicationDbContext dbContext, IChessGameRepositoryConverter chessGameConveter)
        {
            _dbContext = dbContext;
            _chessGameConverter = chessGameConveter;
        }

        /// <inheritdoc />
        public IReadOnlyList<ChessGame> Get(string participantPlayerName, Func<DbChessGame, bool> predicate = null)
        {
            var result = _dbContext.ChessGames
                                   .Include(x => x.InitiatedBy)
                                   .Include(x => x.Opponent)
                                   .Where(x => x.InitiatedBy != null && x.InitiatedBy.UserName == participantPlayerName
                                               || x.Opponent != null && x.Opponent.UserName == participantPlayerName)
                                   .Where(predicate ?? (x => true))
                                   .Select(x => _chessGameConverter.ConvertToChessGame(x))
                                   .ToList()
                                   .AsReadOnly();

            return result;
        }

        /// <inheritdoc />
        public ChessGameRepositoryAddResult Add(string participantPlayerName, Challenge challengeRequest)
        {
            var initiatedBy = _dbContext.Users.SingleOrDefault(x => x.UserName == participantPlayerName);

            if(initiatedBy == null)
            {
                return new ChessGameRepositoryAddResult
                {
                    RequestResult = ChallengeRequestResults.InitiatedByUserNull,
                    NewlyCreatedGame = null
                };
            }

            var opponent = _dbContext.Users.SingleOrDefault(x => x.UserName == challengeRequest.Opponent);

            if (opponent == null)
            {
                return new ChessGameRepositoryAddResult
                {
                    RequestResult = ChallengeRequestResults.OpponentNull,
                    NewlyCreatedGame = null
                };
            }

            var now = DateTime.Now.ToUniversalTime();

            // Randomize sides
            var players = new[] { initiatedBy, opponent }.OrderBy(x => Guid.NewGuid()).ToArray();
            var white = players[0];
            var black = players[1];

            var newGame = new DbChessGame()
            {
                ChallengeDate = now,
                InitiatedBy = initiatedBy,
                Opponent = opponent,
                WhitePlayer = white,
                BlackPlayer = black,
                Name = $"{initiatedBy.UserName} vs {opponent.UserName}",
                LastMoveDate = now
            };

            var newEntity =_dbContext.Add(newGame).Entity;
            _dbContext.SaveChanges();

            return new ChessGameRepositoryAddResult
            {
                RequestResult = ChallengeRequestResults.OK,
                NewlyCreatedGame = _chessGameConverter.ConvertToChessGameDetails(newEntity)
            };
        }

        /// <inheritdoc />
        public IReadOnlyList<ChessGameDetails> GetDetails(string participantPlayerName, Func<DbChessGame, bool> predicate = null)
        {
            var result = _dbContext.ChessGames
                                   .Include(x => x.InitiatedBy)
                                   .Include(x => x.Opponent)
                                   .Where(x => x.InitiatedBy != null && x.InitiatedBy.UserName == participantPlayerName
                                               || x.Opponent != null && x.Opponent.UserName == participantPlayerName)
                                   .Where(predicate ?? (x => true))
                                   .Select(x => _chessGameConverter.ConvertToChessGameDetails(x))
                                   .ToList()
                                   .AsReadOnly();

            return result;
        }

        /// <inheritdoc />
        public ChessGameRepositoryMoveResult Move(string participantPlayerName, ChessMoveApiModel move)
        {
            var match = _dbContext.ChessGames.Include(x => x.InitiatedBy)
                                             .Include(x => x.Opponent)
                                             .Include(x => x.History)
                                             .Where(x => x.InitiatedBy != null && x.InitiatedBy.UserName == participantPlayerName
                                                           || x.Opponent != null && x.Opponent.UserName == participantPlayerName)
                                             .Where(x => x.Id == move.TargetGameId)
                                             .FirstOrDefault();

            if(match is null)
            {
                return new ChessGameRepositoryMoveResult
                {
                    NewState = null,
                    RequestResult = MoveRequestResults.NoMatchFound
                };
            }

            var players = match.GetPlayerNames();

            var history = match.History
                               .OrderBy(x => x.CreatedAt)
                               .Select(x => _chessGameConverter.CovertToChessMove(x))
                               .ToArray();

            var oldChessGameDetails = _chessGameConverter.ConvertToChessGameDetails(match);

            var game = new ChessRepresentationInitializer().Create();
            var gameMechanism = new ChessMechanism();

            foreach (var moveItem in history)
            {
                game = gameMechanism.ApplyMove(game, moveItem);
            }

            var currentPlayerName = players[game.CurrentPlayer];

            if(currentPlayerName != participantPlayerName)
            {
                return new ChessGameRepositoryMoveResult
                {
                    RequestResult = MoveRequestResults.WrongTurn,
                    NewState = null
                };
            }

            if(!gameMechanism.ValidateMove(game, move.Move))
            {
                return new ChessGameRepositoryMoveResult
                {
                    RequestResult = MoveRequestResults.InvalidMove,
                    NewState = oldChessGameDetails
                };
            }

            var newState = gameMechanism.ApplyMove(game, move.Move);

            var result = new ChessGameDetails()
            {
                BlackPlayer = oldChessGameDetails.BlackPlayer,
                ChallengeDate = oldChessGameDetails.ChallengeDate,
                Id = oldChessGameDetails.Id,
                InitiatedBy = oldChessGameDetails.InitiatedBy,
                LastMoveDate = DateTime.Now.ToUniversalTime(),
                Name = oldChessGameDetails.Name,
                Opponent = oldChessGameDetails.Opponent,
                WhitePlayer = oldChessGameDetails.WhitePlayer,
                Representation = newState
            };

            var newDbMove = _chessGameConverter.CovertToDbChessMove(move.Move);

            _dbContext.ChessGames
                      .Find(match.Id)
                      .History
                      .Add(newDbMove);

            _dbContext.SaveChanges();

            return new ChessGameRepositoryMoveResult
            {
                RequestResult = MoveRequestResults.OK,
                NewState = result
            };
        }
    }
}
