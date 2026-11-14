using Contract.DTO.Feature.Game.Command;

namespace Application.Feature.Game.Command
{
    public class SellItemCommand
    {
        #region Attributes
        #endregion

        #region Properties
        public string UserID { get; }
        public SellItemDTO DTO { get; }
        #endregion

        public SellItemCommand(
            string userId,
            SellItemDTO dto)
        {
            UserID = userId;
            DTO = dto;
        }

        #region Methods
        #endregion
    }
}