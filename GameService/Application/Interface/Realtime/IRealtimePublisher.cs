using Contract.DTO.Feature.Admin.Response;
using Contract.DTO.Feature.Design.Response;
using Contract.DTO.Feature.Game.Response;
using Contract.DTO.Runtime.EntityDomain.Component;

namespace Application.Interface.Realtime
{
    public interface IRealtimePublisher
    {
        // ─────────────────────────────
        // Acted (high frequency)
        // ─────────────────────────────
        Task SendEntityActed(
            string roomSpatialId, 
            EntityActedDTO payload);

        // ─────────────────────────────
        // Vitals & Attributes
        // ─────────────────────────────
        Task SendEntityVitalChanged(
            string roomSpatialId,
            EntityVitalChangedDTO payload);

        // ─────────────────────────────
        // State Synchronization (heavy)
        // ─────────────────────────────
        Task SendPlayerCharacteristicSync(
            string connectionId,
            CharacteristicInstanceDTO payload);

        Task SendInventoryItemChanged(
            string connectionId,
            InventoryItemChangedDTO payload);

        Task SendInventoryCleared(
            string connectionId);

        // ─────────────────────────────
        // Lifecycle (spawn / despawn)
        // ─────────────────────────────
        Task SendEntitySpawned(
            string roomSpatialId,
            EntitySpawnedDTO payload);
        Task SendEntityDespawned(
            string roomSpatialId, 
            string entityId);

        // ─────────────────────────────
        // Appearance (changed) 
        // ─────────────────────────────
        Task SendEntityAppearanceChanged(
            string roomSpatialId,
            EntityAppearanceChangedDTO appearanceChanged);

        // ─────────────────────────────
        // Definition Update Notification
        // ─────────────────────────────
        Task SendDefinitionUpdated(
            UpdateDefinitionNotificationDTO notification);

        // ─────────────────────────────
        // Telemetry
        // ─────────────────────────────
        Task SendTelemetryAlert(
            TelemetryEventDTO payload);

        // ─────────────────────────────
        // Admin Dashboard Updates
        // ─────────────────────────────
        Task SendRoomStateChanged(
            RoomStateChangedDTO payload);
        Task SendRoomSyncChanged(
            RoomSyncChangedDTO payload);
        Task SendUserConnectionChanged(
            UserConnectionChangedDTO payload);
        Task SendUserSessionChanged(
            UserSessionChangedDTO payload);
    }
}