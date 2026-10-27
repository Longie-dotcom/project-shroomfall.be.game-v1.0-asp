namespace Application.Feature.Quest.Command
{
    public class GachaRewardCommand
    {
        #region Attributes
        #endregion

        #region Properties
        public string UserID { get; }
        public string QuestInstanceID { get; }
        #endregion

        public GachaRewardCommand(
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