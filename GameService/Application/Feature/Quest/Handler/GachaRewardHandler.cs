using Application.Feature.Abstraction;
using Application.Feature.Quest.Command;
using Application.Interface.Realtime.Managers;
using Application.Service.MetaService;
using Application.Service.WorldService;
using Domain.DomainException;
using ResponseCode;

namespace Application.Feature.Quest.Handler
{
    public class GachaRewardHandler : IHandler<GachaRewardCommand>
    {
        #region Attributes
        private readonly QuestService questService;
        private readonly ISessionManager sessionManager;
        private readonly WorldContext worldContext;
        #endregion

        #region Properties
        #endregion

        public GachaRewardHandler(
            QuestService questService,
            ISessionManager sessionManager,
            WorldContext worldContext)
        {
            this.questService = questService;
            this.sessionManager = sessionManager;
            this.worldContext = worldContext;
        }

        #region Methods
        public async Task Handle(
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
            if(!questService.TryClaimQuestReward(playerInstance, command.QuestInstanceID))
                throw new InternalException(
                    ApplicationCode.QuestHandlerCode.CanNotReceiveReward,
                    $"Player with User ID: {command.UserID} - instance ID: {playerInstanceId} can not receive reward");
        }
        #endregion
    }
}