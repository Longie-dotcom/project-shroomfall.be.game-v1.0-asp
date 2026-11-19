using Application.Feature.Abstraction;
using Application.Feature.Quest.Command;
using Application.Interface.Realtime.Managers;
using Application.Service.MetaService;
using Application.Service.WorldService;
using AutoMapper;
using Contract.DTO.Runtime.MetaDomain;
using Domain.DomainException;
using ResponseCode;

namespace Application.Feature.Quest.Handler
{
    public class GachaRewardHandler : IHandler<GachaRewardCommand, ItemInstanceDTO>
    {
        #region Attributes
        private readonly IMapper mapper;
        private readonly QuestService questService;
        private readonly ISessionManager sessionManager;
        private readonly WorldContext worldContext;
        #endregion

        #region Properties
        #endregion

        public GachaRewardHandler(
            IMapper mapper,
            QuestService questService,
            ISessionManager sessionManager,
            WorldContext worldContext)
        {
            this.mapper = mapper;
            this.questService = questService;
            this.sessionManager = sessionManager;
            this.worldContext = worldContext;
        }

        #region Methods
        public async Task<ItemInstanceDTO> Handle(
            GachaRewardCommand command)
        {
            // Validate ownership
            var playerInstanceId = sessionManager.Get(command.UserID);
            if (string.IsNullOrEmpty(playerInstanceId))
                throw new Unauthorized(
                    ApplicationCode.QuestHandlerCode.PlayerInstanceIDNotFoundGacha,
                    $"Player instance ID is not found with User ID: {command.UserID}");

            // Retrieve player instance
            var playerInstance = worldContext.GetEntity(playerInstanceId);
            if (playerInstance == null)
                throw new InternalException(
                    ApplicationCode.QuestHandlerCode.PlayerInstanceNotFoundGacha,
                    $"Player instance is not found with User ID: {command.UserID} - instance ID: {playerInstanceId}");

            // Claim quest reward
            if(!questService.TryClaimQuestReward(playerInstance, command.QuestInstanceID, out var reward))
                throw new InternalException(
                    ApplicationCode.QuestHandlerCode.CanNotReceiveReward,
                    $"Player with User ID: {command.UserID} - instance ID: {playerInstanceId} can not receive reward");

            return await Task.FromResult(mapper.Map<ItemInstanceDTO>(reward));
        }
        #endregion
    }
}