using Contract.DTO.Feature.Quest.Command;

namespace Application.Feature.Quest.Command
{
    public class QueryQuestsCommand
    {
        #region Attributes
        #endregion

        #region Properties
        public string UserID { get; }
        public QuestInstanceQueryDTO DTO { get; }
        #endregion

        public QueryQuestsCommand(
            string userId,
            QuestInstanceQueryDTO dto)
        {
            UserID = userId;
            DTO = dto;
        }

        #region Methods
        #endregion
    }
}