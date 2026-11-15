using Contract.DTO.Feature.Game.Command;

namespace Application.Feature.Game.Command
{
    public class DropItemCommand
    {
        #region Attributes
        #endregion

        #region Properties
        public string UserID { get; }
        public DropItemDTO DTO { get; }
        #endregion

        public DropItemCommand(
            string userId,
            DropItemDTO dto)
        {
            UserID = userId;
            DTO = dto;
        }

        #region Methods
        #endregion
    }
}
