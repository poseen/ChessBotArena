﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BoardGame.Service.Models.Api.ChessGamesControllerModels;
using BoardGame.Service.Repositories;

namespace Service.Controllers.Api
{
    /// <summary>
    /// Controller used to manage the games.
    /// </summary>
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Route("api/Games")]
    public class ChessGamesController : Controller
    {
        private readonly IChessGameRepository _repository;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChessGamesController"/> class.
        /// </summary>
        /// <param name="repository">The repository to manage chess game (matches) data.</param>
        /// <param name="logger">The logger used to log errors and warnings.</param>
        public ChessGamesController(IChessGameRepository repository, ILogger<ChessGamesController> logger)
        {
            _repository = repository;
            this.logger = logger;
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
            var chessgames = _repository.Get(GetCurrentUser());
            return Ok(chessgames);
        }

        /// <summary>
        /// Returns the details of the selected chess game for the current user.
        /// </summary>
        /// <returns>The detailed view of the selected game.</returns>
        /// <response code="200">Returns the detailed view of the selected game.</response>
        /// <response code="400">Returns empty if the given id is not a GUID.</response>
        /// <response code="401">If there is an authentication error.</response>
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

            var chessgames = _repository.GetDetails(GetCurrentUser(), x => x.Id == gid);

            if(chessgames.Count != 1)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, "Multiple chess games with same id detected!");
            }

            return Ok(chessgames.First());
        }

        /// <summary>
        /// Tries to create a new game according to the incoming challenge.
        /// </summary>
        /// <returns>List of games for the current user.</returns>
        /// <response code="200">Returns HTTP 200 OK if it's succesful.</response>
        /// <response code="400">Returns HTTP 400 Bad Request and the error result if the request isn't valid.</response>
        /// <response code="401">If there is an authentication error.</response>
        /// <response code="500">If there is a server error.</response>
        [ProducesResponseType(typeof(ChallengeRequestResults), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ChallengeRequestResults), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        [HttpPost]
        public IActionResult Challenge([FromBody]Challenge challenge)
        {
            var result = _repository.Add(challenge);

            switch (result.RequestResult)
            {
                case ChallengeRequestResults.OK:
                    return Created(new Uri($"{Request.Path}/{result.NewlyCreatedGame.Id}", UriKind.Relative), result.NewlyCreatedGame);
                
                case ChallengeRequestResults.InitiatedByUserNull:
                case ChallengeRequestResults.OpponentNull:
                    return BadRequest(result);

                default:
                    return StatusCode((int)HttpStatusCode.InternalServerError, result);
            }
        }

        private string GetCurrentUser()
        {
            return User?.Identity?.Name;
        }
    }
}