using Application.Feature.Abstraction;
using Application.Feature.Quest.Command;
using Application.Interface.Realtime.Managers;
using Application.Service.MetaService;
using Application.Service.WorldService;
using Contract.DTO.Runtime.MetaDomain;
using Domain.DomainException;
using ResponseCode;

namespace Application.Feature.Quest.Handler
{
    public class AcceptQuestHandler : IHandler<AcceptQuestCommand, QuestInstanceDTO>
    {
        #region Attributes
        private readonly QuestService questService;
        private readonly ISessionManager sessionManager;
        private readonly WorldContext worldContext;
        #endregion

        #region Properties
        #endregion

        public AcceptQuestHandler(
            QuestService questService,
            ISessionManager sessionManager,
            WorldContext worldContext)
        {
            this.questService = questService;
            this.sessionManager = sessionManager;
            this.worldContext = worldContext;
        }

        #region Methods
        public async Task<QuestInstanceDTO> Handle(
            AcceptQuestCommand command)
        {
            // Validate ownership
            var playerInstanceId = sessionManager.Get(command.UserID);
            if (string.IsNullOrEmpty(playerInstanceId))
                throw new Unauthorized(
                    ApplicationCode.QuestHandlerCode.PlayerInstanceIDNotFoundAcceptQuest,
                    $"Player instance ID is not found with User ID: {command.UserID}");

            // Retrieve player instance
            var playerInstance = worldContext.GetEntity(playerInstanceId);
            if (playerInstance == null)
                throw new InternalException(
                    ApplicationCode.QuestHandlerCode.PlayerInstanceNotFoundAcceptQuest,
                    $"Player instance is not found with User ID: {command.UserID} - instance ID: {playerInstanceId}");

            // Claim quest reward
            var questDto = questService.TryAcceptQuest(playerInstance, command.QuestInstanceID);
            if (questDto == null)
                throw new InternalException(
                    ApplicationCode.QuestHandlerCode.CanNotAcceptQuest,
                    $"Player with User ID: {command.UserID} - instance ID: {playerInstanceId} can not accept quest");

            return questDto;
        }
        #endregion
    }
}