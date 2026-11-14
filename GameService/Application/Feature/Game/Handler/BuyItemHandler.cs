using Application.Feature.Abstraction;
using Application.Feature.Game.Command;
using Application.Interface.Realtime.Managers;
using Application.Service.MetaService;
using Application.Service.WorldService;
using Domain.DomainException;
using ResponseCode;

namespace Application.Feature.Game.Handler
{
    public class BuyItemHandler : IHandler<BuyItemCommand>
    {
        #region Attributes
        private readonly WorldContext worldContext;
        private readonly ISessionManager sessionManager;
        private readonly ItemService itemService;
        #endregion

        #region Properties
        #endregion

        public BuyItemHandler(
            WorldContext worldContext,
            ISessionManager sessionManager,
            ItemService itemService)
        {
            this.worldContext = worldContext;
            this.sessionManager = sessionManager;
            this.itemService = itemService;
        }

        #region Methods
        public Task Handle(
            BuyItemCommand command)
        {
            var dto = command.DTO;

            // Validate session existence
            var playerInstanceId = sessionManager.Get(command.UserID);
            if (string.IsNullOrWhiteSpace(playerInstanceId))
                throw new Unauthorized(
                    ApplicationCode.GameHandlerCode.BuyItemSessionNotFound,
                    $"User with user ID: {command.UserID} has no session");

            // Validate player instance existence
            var player = worldContext.GetEntity(playerInstanceId);
            if (player == null)
                throw new BadRequest(
                    ApplicationCode.GameHandlerCode.BuyItemPlayerInstanceNotFound,
                    $"User with user ID: {command.UserID} has no player instance");

            itemService.Buy(player, dto.ItemDefinitionID);

            return Task.CompletedTask;
        }
        #endregion
    }
}
