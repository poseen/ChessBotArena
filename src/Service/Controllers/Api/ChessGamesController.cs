﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using BoardGame.Game.Chess.Moves;
using BoardGame.Model.Api.ChessGamesControllerModels;
using BoardGame.Service.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BoardGame.Service.Controllers.Api
{
    /// <summary>
    /// Controller used to manage the games.
    /// </summary>
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Route("api/Games")]
    public class ChessGamesController : BaseController
    {
        private readonly IChessGameRepository _repository;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChessGamesController"/> class.
        /// </summary>
        /// <param name="repository">The repository to manage chess game (matches) data.</param>
        /// <param name="logger">The logger used to log errors and warnings.</param>
        public ChessGamesController(IChessGameRepository repository, ILogger<ChessGamesController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        /// <summary>
        /// Returns the list of chess games for the current user.
        /// </summary>
        /// <returns>List of games for the current user.</returns>
        /// <response code="200">Returns the list of games of the current user..</response>
        /// <response code="401">If there is an authentication error.</response>
        /// <response code="500">If there is a server error.</response>
        [ProducesResponseType(typeof(IEnumerable<ChessGame>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [HttpGet]
        public IActionResult GetChessGames()
        {
            var chessGames = _repository.GetList(GetCurrentUser());

            _logger.LogInformation($"{GetCurrentUser()} queried a list of chess games and found {chessGames.Count}.");

            return Ok(chessGames);
        }

        /// <summary>
        /// Returns the list of chess games for the current user.
        /// </summary>
        /// <returns>List of games (with details) for the current user.</returns>
        /// <response code="200">Returns the list of games of the current user..</response>
        /// <response code="401">If there is an authentication error.</response>
        /// <response code="500">If there is a server error.</response>
        [ProducesResponseType(typeof(IEnumerable<ChessGameDetails>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [HttpGet("detailed")]
        public IActionResult GetChessGamesWithDetails()
        {
            var chessGames = _repository.GetListWithDetails(GetCurrentUser());

            _logger.LogInformation($"{GetCurrentUser()} queried a list of chess games (with details) and found {chessGames.Count}.");

            return Ok(chessGames);
        }

        /// <summary>
        /// Returns the details of the selected chess game for the current user.
        /// </summary>
        /// <returns>The detailed view of the selected game.</returns>
        /// <response code="200">Returns the detailed view of the selected game.</response>
        /// <response code="400">Returns empty if the given id is not a GUID.</response>
        /// <response code="401">If there is an authentication error.</response>
        /// <response code="409">If there are multiple games with the same ID.</response>
        /// <response code="500">If there is a server error.</response>
        [ProducesResponseType(typeof(ChessGameDetails), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [HttpGet("{id}")]
        public IActionResult GetChessGame(string id)
        {
            Guid gid;
            bool validId = Guid.TryParse(id, out gid);

            if(!validId)
            {
                return BadRequest();
            }

            var chessGameDetails = _repository.GetDetails(GetCurrentUser(), gid);

            if (chessGameDetails == null)
            {
                _logger.LogWarning($"{GetCurrentUser()} queried the game with id {id} but no game was found.");
                return NotFound();
            }

            _logger.LogInformation($"{GetCurrentUser()} queried the details for game with id {id}.");
            return Ok(chessGameDetails);
        }

        /// <summary>
        /// Tries to create a new game according to the incoming challenge.
        /// </summary>
        /// <returns>List of games for the current user.</returns>
        /// <response code="200">Returns HTTP 200 OK if it's successful.</response>
        /// <response code="400">Returns HTTP 400 Bad Request and the error result if the request isn't valid.</response>
        /// <response code="401">If there is an authentication error.</response>
        /// <response code="500">If there is a server error.</response>
        [ProducesResponseType(typeof(ChessGame), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ChallengeRequestResult), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        [HttpPost]
        public IActionResult Challenge([FromBody]ChallengeRequest challengeRequest)
        {
            var result = _repository.Add(GetCurrentUser(), challengeRequest);

            switch (result.RequestResult)
            {
                case ChallengeRequestResultStatuses.Ok:
                    _logger.LogInformation($"{GetCurrentUser()} has sent a challenge request to user with ID {challengeRequest.Opponent}.");
                    return Created(new Uri($"{Request.Path}{result.NewlyCreatedGame.Id}", UriKind.Relative), result.NewlyCreatedGame);
                
                case ChallengeRequestResultStatuses.InitiatedByUserNull:
                    _logger.LogError($"The challenger user ({GetCurrentUser()}) could not be found.");
                    return BadRequest(result);

                case ChallengeRequestResultStatuses.OpponentNull:
                    _logger.LogWarning($"{GetCurrentUser()} has sent a challenge request to user with ID {challengeRequest.Opponent} but opponent couldn't be found.");
                    return BadRequest(result);

                default:
                    return InternalServerError(result);
            }
        }

        /// <summary>
        /// Tries to apply a move.
        /// </summary>
        /// <returns>Returns new state of the game if successful. Otherwise HTTP error code.</returns>
        /// <response code="200">If it's successful.</response>
        /// <response code="400">If the request isn't valid.</response>
        /// <response code="401">If there is an authentication error.</response>
        /// <response code="404">If the match couldn't be found.</response>
        /// <response code="409">If the move is an illegal move for any reasons.</response>
        /// <response code="500">If there is a server error.</response>
        [ProducesResponseType(typeof(ChessGameMoveResult), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ChessGameMoveResult), (int)HttpStatusCode.Conflict)]
        [ProducesResponseType(typeof(Guid), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        [HttpPut("{gameIdString}")]
        public IActionResult Move(string gameIdString, [FromBody]BaseMove move)
        {
            bool validId = Guid.TryParse(gameIdString, out Guid gid);

            if (!validId)
            {
                _logger.LogWarning($"{GetCurrentUser()} has sent a move to the game with ID {gameIdString} but the ID is not a valid GUID.");
                return BadRequest("Game ID is not valid.");
            }

            var result = _repository.Move(GetCurrentUser(), gid, move);

            switch (result.MoveRequestResultStatus)
            {
                case ChessGameMoveRequestResultStatuses.Ok:
                    _logger.LogInformation($"{GetCurrentUser()} has sent a move to the game with ID {gameIdString}.");
                    return Ok(result);

                case ChessGameMoveRequestResultStatuses.WrongTurn:
                    _logger.LogInformation($"{GetCurrentUser()} has sent a move to the game with ID {gameIdString} in the wrong turn.");
                    return Forbidden(result);

                case ChessGameMoveRequestResultStatuses.InvalidMove:
                    _logger.LogInformation($"{GetCurrentUser()} has sent a move to the game with ID {gameIdString} but it was an invalid move.");
                    return Forbidden(result);

                case ChessGameMoveRequestResultStatuses.NoMatchFound:
                    _logger.LogWarning($"{GetCurrentUser()} has sent a move to the game with ID {gameIdString} but the match was not found.");
                    return NotFound(gameIdString);

                case ChessGameMoveRequestResultStatuses.MultipleMatchesFound:
                    _logger.LogError($"{GetCurrentUser()} has sent a move to the game with ID {gameIdString} but multiple matches were found.");
                    return Conflict(result);

                default:
                    return InternalServerError();
            }
        }
    }
}
