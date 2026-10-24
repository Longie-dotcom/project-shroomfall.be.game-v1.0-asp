using API.Helper;
using Application.Feature.Abstraction;
using Application.Feature.Admin.Command;
using Contract.DTO.Runtime.WorldDomain;
using Contract.Enum.IdentityDomain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        #region Attributes
        private readonly IDispatcher dispatcher;
        #endregion

        #region Properties
        #endregion

        public AdminController(
            IDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        #region Methods
        [Authorize(Roles = nameof(Role.Admin))]
        [HttpGet("room-spatials")]
        public async Task<IActionResult> GetRoomSpatials()
        {
            var (userId, steamId, role) = ClaimReader.GetIdentity(User);

            var result = await dispatcher.Send<FetchRoomSpatialsCommand, List<RoomSpatialDTO>>(
                new FetchRoomSpatialsCommand(userId)
            );

            return Ok(result);
        }

        [Authorize(Roles = nameof(Role.Admin))]
        [HttpGet("room-instance/{roomSpatialId}")]
        public async Task<IActionResult> GetRoomInstance(
            string roomSpatialId)
        {
            var (userId, steamId, role) = ClaimReader.GetIdentity(User);

            var result = await dispatcher.Send<FetchRoomInstanceCommand, RoomInstanceDTO>(
                new FetchRoomInstanceCommand(userId, roomSpatialId)
            );

            return Ok(result);
        }
        #endregion
    }
}