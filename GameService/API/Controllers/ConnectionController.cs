using API.Helper;
using Application.Feature.Abstraction;
using Application.Feature.Connection.Command;
using Contract.DTO.Feature.Connection.Command;
using Contract.DTO.Feature.Connection.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConnectionController : ControllerBase
    {
        #region Attributes
        private readonly IDispatcher dispatcher;
        #endregion

        #region Properties
        #endregion

        public ConnectionController(
            IDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        #region Methods
        [Authorize]
        [HttpPost("session")]
        public async Task<IActionResult> CreateSession(
            [FromBody] CreateSessionDTO dto)
        {
            var (userId, googleId, role) = ClaimReader.GetIdentity(User);

            await dispatcher.Send<CreateSessionCommand>(
                new CreateSessionCommand(userId, dto)
            );

            return Ok();
        }

        [Authorize]
        [HttpGet("sessions")]
        public async Task<IActionResult> FetchSessions()
        {
            var (userId, googleId, role) = ClaimReader.GetIdentity(User);

            var result = await dispatcher.Send<FetchSessionCommand, ExistedSessionDTO>(
                new FetchSessionCommand(userId)
            );

            return Ok(result);
        }

        [Authorize]
        [HttpPost("session/load")]
        public async Task<IActionResult> LoadSession(
            [FromBody] LoadSessionDTO dto)
        {
            var (userId, googleId, role) = ClaimReader.GetIdentity(User);

            var result = await dispatcher.Send<LoadSessionCommand, SaveGameDTO>(
                new LoadSessionCommand(userId, dto)
            );

            return Ok(result);
        }

        [Authorize]
        [HttpPost("session/unload")]
        public async Task<IActionResult> UnloadSession()
        {
            var (userId, googleId, role) = ClaimReader.GetIdentity(User);

            await dispatcher.Send<UnloadSessionCommand>(
                new UnloadSessionCommand(userId)
            );

            return Ok();
        }
        #endregion
    }
}