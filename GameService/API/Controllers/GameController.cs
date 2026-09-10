using API.Helper;
using Application.Feature.Abstraction;
using Application.Feature.Game.Command;
using Contract.DTO.Feature.Connection.Response;
using Contract.DTO.Feature.Design.Command;
using Contract.DTO.Feature.Design.Response;
using Contract.DTO.Feature.Game.Command;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GameController : ControllerBase
    {
        #region Attributes
        private readonly IDispatcher dispatcher;
        #endregion

        #region Properties
        #endregion

        public GameController(
            IDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        #region Methods
        [Authorize]
        [HttpPost("back-home")]
        public async Task<IActionResult> BackHome()
        {
            var (userId, _, _) = ClaimReader.GetIdentity(User);

            var snapshot = await dispatcher.Send<BackHomeCommand, SaveGameDTO>(
                new BackHomeCommand(userId)
            );

            return Ok(snapshot);
        }

        [Authorize]
        [HttpPost("enter-hub/{hubRoomSpatialId}")]
        public async Task<IActionResult> EnterHub(
            [FromRoute] string hubRoomSpatialId)
        {
            var (userId, _, _) = ClaimReader.GetIdentity(User);

            var snapshot = await dispatcher.Send<EnterHubCommand, SaveGameDTO>(
                new EnterHubCommand(userId, hubRoomSpatialId)
            );

            return Ok(snapshot);
        }

        [Authorize]
        [HttpPut("appearance")]
        public async Task<IActionResult> UpdateAppearance(
            [FromBody] UpdatePlayerAppearanceDTO dto)
        {
            var (userId, steamId, role) = ClaimReader.GetIdentity(User);

            await dispatcher.Send<UpdateAppearanceCommand>(
                new UpdateAppearanceCommand(userId, dto)
            );

            return NoContent();
        }

        [Authorize]
        [HttpPost("use-item")]
        public async Task<IActionResult> UseItem(
            [FromBody] UseItemDTO dto)
        {
            var (userId, steamId, role) = ClaimReader.GetIdentity(User);

            await dispatcher.Send<UseItemCommand>(
                new UseItemCommand(userId, dto)
            );

            return NoContent();
        }

        [Authorize]
        [HttpGet("{version}")]
        public async Task<ActionResult<DefinitionSnapshotDTO?>> UserRefresh(
            string version)
        {
            var (userId, steamId, role) = ClaimReader.GetIdentity(User);

            var result = await dispatcher.Send<UserRefreshCommand, DefinitionSnapshotDTO?>(
                new UserRefreshCommand(userId, new UserRefreshDTO { DefinitionVersion = version })
            );

            return Ok(result);
        }
        #endregion
    }
}