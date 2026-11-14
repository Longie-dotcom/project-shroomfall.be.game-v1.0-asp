using Contract.DTO.Feature.Game.Command;

namespace Application.Feature.Game.Command
{
    public class BuyItemCommand
    {
        #region Attributes
        #endregion

        #region Properties
        public string UserID { get; }
        public BuyItemDTO DTO { get; }
        #endregion

        public BuyItemCommand(
            string userId,
            BuyItemDTO dto)
        {
            UserID = userId;
            DTO = dto;
        }

        #region Methods
        #endregion
    }
}