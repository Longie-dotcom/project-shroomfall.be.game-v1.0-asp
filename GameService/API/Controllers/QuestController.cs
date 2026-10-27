using API.Helper;
using Application.Feature.Abstraction;
using Application.Feature.Quest.Command;
using Contract.DTO.Abstraction;
using Contract.DTO.Feature.Quest.Command;
using Contract.DTO.Runtime.MetaDomain;
using Contract.Enum.IdentityDomain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestController : ControllerBase
    {
        #region Attributes
        private readonly IDispatcher dispatcher;
        #endregion

        #region Properties
        #endregion

        public QuestController(
            IDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        #region Methods
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> QueryQuests(
            [FromQuery] QuestInstanceQueryDTO dto)
        {
            var (userId, _, _) = ClaimReader.GetIdentity(User);

            var result = await dispatcher.Send<QueryQuestsCommand, PagedResponseDTO<QuestInstanceDTO>>(
                new QueryQuestsCommand(userId, dto)
            );

            return Ok(result);
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<QuestInstanceDTO>> AcceptQuest(
            [FromBody] string questInstanceId)
        {
            var (userId, _, _) = ClaimReader.GetIdentity(User);

            var result = await dispatcher.Send<AcceptQuestCommand, QuestInstanceDTO>(
                new AcceptQuestCommand(userId, questInstanceId)
            );

            return Ok(result);
        }

        [Authorize(Roles = nameof(Role.GameMaster))]
        [HttpPost]
        public async Task<IActionResult> CreateQuest(
            [FromBody] QuestInstanceDTO dto)
        {
            var (userId, _, _) = ClaimReader.GetIdentity(User);

            await dispatcher.Send<CreateQuestCommand>(
                new CreateQuestCommand(userId, dto)
            );

            return Ok();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SubmitQuestResult(
            [FromBody] SubmitQuestResultDTO dto)
        {
            var (userId, _, _) = ClaimReader.GetIdentity(User);

            await dispatcher.Send<SubmitQuestResultCommand>(
                new SubmitQuestResultCommand(userId, dto)
            );

            return Ok();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> GachaReward(
            [FromBody] string questInstanceId)
        {
            var (userId, _, _) = ClaimReader.GetIdentity(User);

            await dispatcher.Send<GachaRewardCommand>(
                new GachaRewardCommand(userId, questInstanceId)
            );

            return Ok();
        }
        #endregion
    }
}