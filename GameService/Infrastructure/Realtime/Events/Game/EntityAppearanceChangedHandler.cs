using Application.Interface.Realtime;
using Application.Interface.Realtime.Events;
using Application.Interface.Realtime.Events.Game;
using Contract.DTO.Feature.Game.Response;

namespace Infrastructure.Realtime.Events.Game
{
    public class EntityAppearanceChangedHandler : IEventHandler
    {
        #region Attributes
        private readonly IRealtimePublisher publisher;
        #endregion

        #region Properties
        #endregion

        public EntityAppearanceChangedHandler(
            IRealtimePublisher publisher)
        {
            this.publisher = publisher;
        }

        #region Methods
        public async Task Handle(
            IEvent @event)
        {
            if (@event is not EntityAppearanceChangedEvent changed)
                return;

            await publisher.SendEntityAppearanceChanged(
                changed.RoomSpatialID,
                new EntityAppearanceChangedDTO()
                {
                    EntityInstanceID = changed.EntityInstanceID,
                    Appearance = changed.Appearance,
                });
        }
        #endregion
    }
}