using Application.Interface.Realtime.Events;
using Application.Interface.Utility;
using Application.Interface.Realtime.Events.Admin;
using Application.Service.WorldService.Creation;
using Application.Service.WorldService.Persistence;
using Contract;
using Domain.DomainException;
using Domain.Runtime.EntityDomain;
using Domain.Runtime.EntityDomain.Component;
using ResponseCode;
using System.Collections.Concurrent;

namespace Application.Service.WorldService
{
    internal enum RoomResidencyState
    {
        Cold,
        Warm,
        Hot
    }

    public enum RoomLifecyclePolicy
    {
        /// <summary>
        /// Lives only in RAM. When evicted, it is destroyed completely. (e.g., Temporary dungeons, mini-games)
        /// </summary>
        Ephemeral,

        /// <summary>
        /// Lives in RAM. When evicted (goes Cold), it must be saved to the database. (e.g., Player personal rooms)
        /// </summary>
        Persistent,

        /// <summary>
        /// Always Hot/Warm. Never evicted, never saved to DB during normal runtime. (e.g., Static Hubs like towns)
        /// </summary>
        Permanent
    }

    internal class RoomNode
    {
        public string RoomSpatialID { get; set; } = string.Empty;
        public RoomResidencyState State { get; set; }
        public RoomLifecyclePolicy Lifecycle { get; set; } // Replaces RoomResidencyPolicy
        public DateTime LastAccessUtc { get; set; }
        public HashSet<string> ActivePlayerInstanceIds { get; } = new();
    }

    public class ResidencyService
    {
        #region Attributes
        private readonly ITelemetryQueue telemetryQueue;
        private readonly IEventBus eventBus;
        private readonly WorldContext worldContext;
        private readonly SnapshotPersistence snapshotPersistence;
        private readonly EntityPersistence entityPersistence;

        private readonly ConcurrentDictionary<string, SemaphoreSlim> roomLocks = new();
        private readonly ConcurrentDictionary<string, RoomNode> nodes = new();

        private readonly TimeSpan warmTTL = TimeSpan.FromSeconds(30);
        private float evictionAccumulator = 0;
        private float telemetryAccumulator = 0;
        #endregion

        #region Properties
        #endregion

        public ResidencyService(
            ITelemetryQueue telemetryQueue,
            IEventBus eventBus,
            WorldContext worldContext,
            SnapshotPersistence snapshotPersistence,
            EntityPersistence entityPersistence)
        {
            this.telemetryQueue = telemetryQueue;
            this.eventBus = eventBus;
            this.worldContext = worldContext;
            this.snapshotPersistence = snapshotPersistence;
            this.entityPersistence = entityPersistence;
        }

        #region Methods
        public async Task PlayerJoinRoomAsync(
            string roomId,
            string playerInstanceId)
        {
            var roomLock = GetRoomLock(roomId);
            await roomLock.WaitAsync();
            try
            {
                var node = GetOrCreate(roomId);
                var oldState = node.State;

                // Thread-safely register player presence
                node.ActivePlayerInstanceIds.Add(playerInstanceId);
                node.LastAccessUtc = DateTime.UtcNow;

                // Automate State Escalation: Elevate room state to Hot if it wasn't already
                if (node.State != RoomResidencyState.Hot)
                {
                    node.State = RoomResidencyState.Hot;

                    eventBus.Publish(new RoomStateChangedEvent(
                        node.RoomSpatialID,
                        oldState.ToString(),
                        node.State.ToString()));
                }
            }
            finally
            {
                roomLock.Release();
            }
        }

        public async Task PlayerLeaveRoomAsync(
            string roomId,
            string playerInstanceId)
        {
            var roomLock = GetRoomLock(roomId);
            await roomLock.WaitAsync();
            try
            {
                if (!nodes.TryGetValue(roomId, out var node))
                    return;
                var oldState = node.State;

                // Thread-safely remove player presence
                node.ActivePlayerInstanceIds.Remove(playerInstanceId);
                node.LastAccessUtc = DateTime.UtcNow;

                // Automate State Downgrade: If the room became completely empty, drop it to Warm
                if (node.ActivePlayerInstanceIds.Count == 0 && node.State == RoomResidencyState.Hot)
                {
                    if (node.Lifecycle == RoomLifecyclePolicy.Permanent)
                        return; // Permanent rooms ignore automatic dynamic downgrades

                    node.State = RoomResidencyState.Warm;

                    eventBus.Publish(new RoomStateChangedEvent(
                        node.RoomSpatialID,
                        oldState.ToString(),
                        node.State.ToString()));
                }
            }
            finally
            {
                roomLock.Release();
            }
        }

        public async Task PlayerQuitGame(
            string roomId,
            EntityInstance player)
        {
            // Execute leave room logic
            await PlayerLeaveRoomAsync(roomId, player.ID);

            // Save frozen data instance to cold storage
            await entityPersistence.SaveManyAsync(new List<EntityInstance>() { player });
        }

        public async Task<EntityInstance> EnsurePlayerExisted(
            string playerInstanceId)
        {
            // Try to get the LIVE player from RAM
            var player = worldContext.GetEntity(playerInstanceId);
            if (player != null)
                return player;

            // Fallback to DB just to find out where they are
            var coldPlayer = await entityPersistence.LoadEntityAsync(playerInstanceId);
            if (coldPlayer != null)
                return coldPlayer;

            throw new InternalException(
                ApplicationCode.ResidencyServiceCode.PlayerNotFoundInSystem,
                $"Player instance {playerInstanceId} not found in memory or database.");
        }

        public void EnsurePlayerResident(
            EntityInstance player)
        {
            if (worldContext.GetEntity(player.ID) != null)
                return;

            worldContext.AddEntity(player);
        }

        public async Task<RoomInstance> EnsureRoomLoaded(
            string roomSpatialId)
        {
            var roomLock = GetRoomLock(roomSpatialId);
            await roomLock.WaitAsync();

            RoomNode node;
            RoomInstance? roomInstance;
            var oldState = RoomResidencyState.Cold;

            try
            {
                node = GetOrCreate(roomSpatialId);
                oldState = node.State;

                roomInstance = TryGetRoomInstance(roomSpatialId);
                if (roomInstance != null)
                    return roomInstance;

                roomInstance = await snapshotPersistence.LoadRoomInstanceAsync(roomSpatialId);
                if (roomInstance == null)
                    throw new InternalException(
                        ApplicationCode.ResidencyServiceCode.RoomInstanceNotFoundInPersistence,
                        $"Residency synchronization failed. Cold state Room '{roomSpatialId}' contains no archived record state inside instance persistence layer.");

                // Players enter the world only via EnsurePlayerResident; a stale saved copy here would overwrite the live player by ID
                roomInstance.Entities.RemoveAll(e => e.GetComponent<OwnershipInstance>() != null);

                worldContext.Load(roomInstance);
                node.State = RoomResidencyState.Warm;
                node.LastAccessUtc = DateTime.UtcNow;

                eventBus.Publish(new RoomSyncChangedEvent(
                    roomInstance.Room,
                    true));

                eventBus.Publish(new RoomStateChangedEvent(
                    roomSpatialId,
                    RoomResidencyState.Cold.ToString(),
                    node.State.ToString()));
            }
            finally
            {
                roomLock.Release();
            }

            return roomInstance;
        }

        public RoomInstance RegisterRuntimeRoom(
            RoomInstance roomInstance,
            RoomLifecyclePolicy lifecycle)
        {
            var roomLock = GetRoomLock(roomInstance.Room.ID);
            roomLock.Wait();

            try
            {
                var node = GetOrCreate(roomInstance.Room.ID);
                node.Lifecycle = lifecycle;

                if (node.State != RoomResidencyState.Cold)
                    return TryGetRoomInstance(roomInstance.Room.ID)!;

                // If it's persistent and has no players yet, keep it Cold and save directly
                if (node.Lifecycle == RoomLifecyclePolicy.Persistent && node.ActivePlayerInstanceIds.Count == 0)
                {
                    node.State = RoomResidencyState.Cold;
                    node.LastAccessUtc = DateTime.UtcNow;

                    // Save straight to persistence without a RAM round-trip
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await snapshotPersistence.SaveRoomInstanceAsync(roomInstance);
                        }
                        catch (Exception ex)
                        {
                            telemetryQueue.EnqueueAlert(
                                ApplicationCode.ResidencyServiceCode.RoomSnapshotPersistenceFailed,
                                $"Failed to persist initial state for persistent room '{roomInstance.Room.ID}'. Exception: {ex.Message}",
                                TelemetrySeverity.Error);
                        }
                    });
                }
                else
                {
                    // Load into RAM as Warm/Hot
                    worldContext.Load(roomInstance);
                    node.State = RoomResidencyState.Warm;
                    node.LastAccessUtc = DateTime.UtcNow;

                    eventBus.Publish(new RoomSyncChangedEvent(
                        roomInstance.Room,
                        true));

                    eventBus.Publish(new RoomStateChangedEvent(
                        roomInstance.Room.ID,
                        RoomResidencyState.Cold.ToString(),
                        node.State.ToString()));
                }
            }
            finally
            {
                roomLock.Release();
            }

            return roomInstance;
        }

        public async Task Tick(
            float dt)
        {
            evictionAccumulator += dt;
            telemetryAccumulator += dt;

            // Core State Processing Loop
            if (evictionAccumulator >= Constraint.RESIDENCY_TICK_PER_SECOND)
            {
                evictionAccumulator = 0;
                var now = DateTime.UtcNow;

                foreach (var node in nodes.Values)
                {
                    if (node.Lifecycle == RoomLifecyclePolicy.Permanent)
                        continue;

                    if (node.State == RoomResidencyState.Hot)
                        continue;

                    if (node.State == RoomResidencyState.Warm && now - node.LastAccessUtc > warmTTL)
                        await EvictToCold(node);
                }
            }

            // Metrics Accumulation & Aggregation Telemetry
            if (telemetryAccumulator >= Constraint.RESIDENCY_REPORT_PER_SECOND)
            {
                telemetryAccumulator = 0;

                int coldCount = 0;
                int warmCount = 0;
                int hotCount = 0;

                foreach (var node in nodes.Values)
                {
                    switch (node.State)
                    {
                        case RoomResidencyState.Cold: coldCount++; break;
                        case RoomResidencyState.Warm: warmCount++; break;
                        case RoomResidencyState.Hot: hotCount++; break;
                    }
                }

                // Append telemetry instance data to tracking stream safely
                telemetryQueue.EnqueueAlert(
                    ApplicationCode.ResidencyServiceCode.StateHeartbeatReport,
                    $"Residency performance update. Active clusters tracked -> Hot: {hotCount} | Warm: {warmCount} | Cold: {coldCount} (Total Tracked Rooms: {nodes.Count})",
                    TelemetrySeverity.Info);
            }
        }

        private async Task EvictToCold(
            RoomNode node)
        {
            var roomLock = GetRoomLock(node.RoomSpatialID);
            await roomLock.WaitAsync();

            try
            {
                // 1. Double-check state under lock
                var oldState = node.State;
                if (oldState == RoomResidencyState.Cold)
                    return;

                // 2. Temporarily remove from active RAM
                var roomInstance = worldContext.Unload(node.RoomSpatialID);
                if (roomInstance != null)
                {
                    try
                    {
                        await snapshotPersistence.SaveRoomInstanceAsync(roomInstance);

                        node.State = RoomResidencyState.Cold;
                        node.LastAccessUtc = DateTime.UtcNow;

                        eventBus.Publish(new RoomSyncChangedEvent(
                            roomInstance.Room,
                            false));

                        eventBus.Publish(new RoomStateChangedEvent(
                            node.RoomSpatialID,
                            oldState.ToString(),
                            node.State.ToString()));
                    }
                    catch
                    {
                        worldContext.Load(roomInstance); 
                        throw;
                    }
                }
            }
            finally
            {
                roomLock.Release();
            }
        }

        public RoomInstance? TryGetRoomInstance(
            string roomSpatialId)
        {
            var node = GetOrCreate(roomSpatialId);

            if (node.State == RoomResidencyState.Cold)
                return null;

            node.LastAccessUtc = DateTime.UtcNow;

            var room = worldContext.GetRoom(roomSpatialId);
            if (room == null)
                throw new InternalException(
                    ApplicationCode.ResidencyServiceCode.RoomSpatialNotFoundInRuntime,
                    $"Residency synchronization failed. Room '{roomSpatialId}' is state marked '{node.State}' but could not be located inside runtime memory context.");

            return new RoomInstance
            {
                Room = room,
                Entities = worldContext
                    .GetEntitiesByRoom(roomSpatialId)
                    .ToList()
            };
        }

        private RoomNode GetOrCreate(
            string roomId)
        {
            if (!nodes.TryGetValue(roomId, out var node))
            {
                node = new RoomNode
                {
                    RoomSpatialID = roomId,
                    State = RoomResidencyState.Cold,
                    LastAccessUtc = DateTime.UtcNow
                };

                nodes[roomId] = node;
            }

            return node;
        }

        private SemaphoreSlim GetRoomLock(
            string roomId)
        {
            return roomLocks.GetOrAdd(roomId, _ => new SemaphoreSlim(1, 1));
        }
        #endregion
    }
}