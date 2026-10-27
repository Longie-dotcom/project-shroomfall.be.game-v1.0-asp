using Contract.DTO.Feature.Quest.Command;

namespace Application.Feature.Quest.Command
{
    public class SubmitQuestResultCommand
    {
        #region Attributes
        #endregion

        #region Properties
        public string UserID { get; }
        public SubmitQuestResultDTO DTO { get; }
        #endregion

        public SubmitQuestResultCommand(
            string userId,
            SubmitQuestResultDTO dto) 
        {
            UserID = userId;
            DTO = dto;
        }

        #region Methods
        #endregion
    }
}