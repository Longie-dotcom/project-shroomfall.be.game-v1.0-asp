using Contract.DTO.Runtime.MetaDomain;

namespace Application.Feature.Quest.Command
{
    public class CreateQuestCommand
    {
        #region Attributes
        #endregion

        #region Properties
        public string UserID { get; }
        public QuestInstanceDTO DTO { get; }
        #endregion

        public CreateQuestCommand(
            string userId,
            QuestInstanceDTO dto)
        {
            UserID = userId;
            DTO = dto;
        }

        #region Methods
        #endregion
    }
}