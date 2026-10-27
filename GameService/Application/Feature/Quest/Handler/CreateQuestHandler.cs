using Application.Feature.Abstraction;
using Application.Feature.Quest.Command;
using Application.Service.MetaService;

namespace Application.Feature.Quest.Handler
{
    public class CreateQuestHandler : IHandler<CreateQuestCommand>
    {
        #region Attributes
        private readonly QuestService questService;
        #endregion

        #region Properties
        #endregion

        public CreateQuestHandler(
            QuestService questService)
        {
            this.questService = questService;
        }

        #region Methods
        public async Task Handle(
            CreateQuestCommand command)
        {
            questService.StoreQuest(command.DTO);
        }
        #endregion
    }
}