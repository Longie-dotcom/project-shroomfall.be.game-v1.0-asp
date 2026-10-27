using Application.Feature.Abstraction;
using Application.Feature.Quest.Command;
using Application.Interface.Realtime.Managers;
using Application.Service.MetaService;
using Application.Service.WorldService;
using Domain.DomainException;
using ResponseCode;

namespace Application.Feature.Quest.Handler
{
    public class SubmitQuestResultHandler : IHandler<SubmitQuestResultCommand>
    {
        #region Attributes
        private readonly QuestService questService;
        private readonly ISessionManager sessionManager;
        private readonly WorldContext worldContext;
        #endregion

        #region Properties
        #endregion

        public SubmitQuestResultHandler(
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
            SubmitQuestResultCommand command)
        {
            var dto = command.DTO;

            // Validate ownership
            var playerInstanceId = sessionManager.Get(command.UserID);
            if (string.IsNullOrEmpty(playerInstanceId))
                throw new Unauthorized(
                    ApplicationCode.QuestHandlerCode.PlayerInstanceIDNotFoundSubmit,
                    $"Player instance ID is not found with User ID: {command.UserID}");

            // Retrieve player instance
            var playerInstance = worldContext.GetEntity(playerInstanceId);
            if (playerInstance == null)
                throw new InternalException(
                    ApplicationCode.QuestHandlerCode.PlayerInstanceNotFoundSubmit,
                    $"Player instance is not found with User ID: {command.UserID} - instance ID: {playerInstanceId}");

            // Submit quest
            if (!questService.TrySubmitQuestResult(playerInstance, dto.QuestInstanceId, dto.IsCompleted))
                throw new InternalException(
                    ApplicationCode.QuestHandlerCode.CanNotSubmitQuest,
                    $"Player with User ID: {command.UserID} - instance ID: {playerInstanceId} can not submit quest");
        }
        #endregion
    }
}