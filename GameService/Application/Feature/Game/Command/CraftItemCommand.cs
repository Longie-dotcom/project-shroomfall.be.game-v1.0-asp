using Contract.DTO.Feature.Game.Command;

namespace Application.Feature.Game.Command
{
    public class CraftItemCommand
    {
        #region Attributes
        #endregion

        #region Properties
        public string UserID { get; }
        public CraftItemDTO DTO { get; }
        #endregion

        public CraftItemCommand(
            string userId,
            CraftItemDTO dto)
        {
            UserID = userId;
            DTO = dto;
        }

        #region Methods
        #endregion
    }
}