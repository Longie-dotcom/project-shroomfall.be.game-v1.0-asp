using Application.Service.WorldService.Creation;
using Contract;

namespace Application.Service.WorldService
{
    public class BootstrapService
    {
        #region Attributes
        private readonly InitializationService initializationService;
        #endregion

        #region Properties
        #endregion

        public BootstrapService(
            InitializationService initializationService)
        {
            this.initializationService = initializationService;
        }

        #region Methods
        public async Task LoadAsync()
        {
            foreach (var hub in Constraint.STATIC_HUB_ROOM_MAPS)
            {
                initializationService.InitializeRoom(
                    hub.DefinitionKey,
                    hub.SpatialId,
                    RoomLifecyclePolicy.Permanent,
                    null);
            }
        }
        #endregion
    }
}