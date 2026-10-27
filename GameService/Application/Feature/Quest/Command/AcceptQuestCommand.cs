namespace Application.Feature.Quest.Command
{
    public class AcceptQuestCommand
    {
        #region Attributes
        #endregion

        #region Properties
        public string UserID { get; }
        public string QuestInstanceID { get; }
        #endregion

        public AcceptQuestCommand(
            string userId,
            string questInstanceId)
        {
            UserID = userId;
            QuestInstanceID = questInstanceId;
        }

        #region Methods
        #endregion
    }
}