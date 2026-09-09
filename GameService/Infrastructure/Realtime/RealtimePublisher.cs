using Application.Interface.Realtime;
using Contract;
using Contract.DTO.Feature.Admin.Response;
using Contract.DTO.Feature.Design.Response;
using Contract.DTO.Feature.Game.Response;
using Contract.DTO.Runtime.EntityDomain.Component;
using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Realtime
{
    public class RealtimePublisher : IRealtimePublisher
    {
        #region Attributes
        private readonly IHubContext<GameHub> gameHub;
        private readonly IHubContext<AdminHub> adminHub;
        #endregion

        #region Properties
        #endregion

        public RealtimePublisher(
            IHubContext<GameHub> gameHub,
            IHubContext<AdminHub> adminHub)
        {
            this.gameHub = gameHub;
            this.adminHub = adminHub;
        }

        #region Methods
        // ─────────────────────────────
        // Movement (broadcast to room)
        // ─────────────────────────────
        public Task SendEntityActed(
            string roomId,
            EntityActedDTO payload)
        {
            return gameHub.Clients
                .Group(roomId)
                .SendAsync(NetworkMethod.OnEntityActed, payload);
        }

        // ─────────────────────────────
        // Vitals & Attributes
        // ─────────────────────────────
        public Task SendEntityVitalChanged(
            string roomId,
            EntityVitalChangedDTO payload)
        {
            return gameHub.Clients
                .Group(roomId)
                .SendAsync(NetworkMethod.OnEntityVitalChanged, payload);
        }

        // ─────────────────────────────
        // State Synchronization (heavy)
        // ─────────────────────────────
        public Task SendPlayerCharacteristicSync(
            string connectionId,
            CharacteristicInstanceDTO payload)
        {
            return gameHub.Clients
                .Client(connectionId)
                .SendAsync(NetworkMethod.OnPlayerCharacteristicSync, payload);
        }

        public Task SendInventoryItemChanged(
            string connectionId,
            InventoryItemChangedDTO payload)
        {
            return gameHub.Clients
                .Client(connectionId)
                .SendAsync(NetworkMethod.OnInventoryItemChanged, payload);
        }

        public Task SendInventoryCleared(
            string connectionId)
        {
            return gameHub.Clients
                .Client(connectionId)
                .SendAsync(NetworkMethod.OnInventoryCleared);
        }

        // ─────────────────────────────
        // Spawn & Despawn (broadcast to room)
        // ─────────────────────────────
        public Task SendEntitySpawned(
            string roomId,
            EntitySpawnedDTO payload)
        {
            return gameHub.Clients
                .Group(roomId)
                .SendAsync(NetworkMethod.OnEntitySpawned, payload);
        }

        public Task SendEntityDespawned(
            string roomId, 
            string entityId)
        {
            return gameHub.Clients
                .Group(roomId)
                .SendAsync(NetworkMethod.OnEntityDespawned, entityId);
        }

        // ─────────────────────────────
        // Appearance Changed
        // ─────────────────────────────
        public Task SendEntityAppearanceChanged(
            string roomId,
            EntityAppearanceChangedDTO appearanceChanged)
        {
            return gameHub.Clients
                .Group(roomId)
                .SendAsync(NetworkMethod.OnEntityAppearanceChanged, appearanceChanged);
        }

        // ─────────────────────────────
        // Definition update (broadcast)
        // ─────────────────────────────
        public Task SendDefinitionUpdated(
            UpdateDefinitionNotificationDTO notification)
        {
            return gameHub.Clients
                .All
                .SendAsync(NetworkMethod.OnDefinitionUpdated, notification);
        }

        // ─────────────────────────────
        // Telemetry
        // ─────────────────────────────
        public Task SendTelemetryAlert(
            TelemetryEventDTO payload)
        {
            return adminHub.Clients
                .Group(Constraint.ADMIN_REALTIME_GROUP)
                .SendAsync(NetworkMethod.OnTelemetrySended, payload);
        }

        // ─────────────────────────────
        // Admin Dashboard Updates
        // ─────────────────────────────
        public Task SendRoomStateChanged(
            RoomStateChangedDTO payload)
        {
            return adminHub.Clients
                .Group(Constraint.ADMIN_REALTIME_GROUP)
                .SendAsync(NetworkMethod.OnRoomStateChanged, payload);
        }

        public Task SendRoomSyncChanged(
            RoomSyncChangedDTO payload)
        {
            return adminHub.Clients
                .Group(Constraint.ADMIN_REALTIME_GROUP)
                .SendAsync(NetworkMethod.OnRoomSyncChanged, payload);
        }

        public Task SendUserConnectionChanged(
            UserConnectionChangedDTO payload)
        {
            return adminHub.Clients
                .Group(Constraint.ADMIN_REALTIME_GROUP)
                .SendAsync(NetworkMethod.OnUserConnectionChanged, payload);
        }

        public Task SendUserSessionChanged(
            UserSessionChangedDTO payload)
        {
            return adminHub.Clients
                .Group(Constraint.ADMIN_REALTIME_GROUP)
                .SendAsync(NetworkMethod.OnUserSessionChanged, payload);
        }
        #endregion
    }
}