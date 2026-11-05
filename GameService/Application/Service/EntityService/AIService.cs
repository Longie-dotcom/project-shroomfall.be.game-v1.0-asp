using Application.Service.WorldService;
using Application.System.Abstraction;
using Application.System.Queue;
using Contract.Common;
using Contract.Enum.EntityDomain;
using Contract.Enum.MetaDomain.Effect;
using Contract.Enum.MetaDomain.Item;
using Domain.Runtime.EntityDomain;
using Domain.Runtime.EntityDomain.Component;

namespace Application.Service.EntityService
{
    public class AIService : ITickService
    {
        #region Attributes
        private readonly WorldContext worldContext;
        private readonly CollisionService collisionService;
        #endregion

        public AIService(
            WorldContext worldContext,
            CollisionService collisionService)
        {
            this.worldContext = worldContext;
            this.collisionService = collisionService;
        }

        #region Methods
        public void Tick(
            float dt,
            CommandBuffer commandBuffer)
        {
            var entities = worldContext.GetEntities().ToList();

            foreach (var entity in entities)
            {
                var ai = entity.GetComponent<AIInstance>();
                if (ai == null || !ai.IsAIControlled)
                    continue;

                ai.ThinkCooldownRemaining -= dt;
                if (ai.ThinkCooldownRemaining > 0)
                    continue;

                ai.ThinkCooldownRemaining = ai.ThinkInterval;

                // Remember where the entity first thought as its home, the leash anchors to it
                var transform = entity.GetComponent<TransformInstance>();
                if (transform != null)
                    ai.HomePosition ??= new Vector2(transform.Position.X, transform.Position.Y);

                switch (ai.AIState)
                {
                    case AIState.Idle: TickIdle(entity, ai); break;
                    case AIState.Wander: TickWander(entity, ai); break;
                    case AIState.Chase: TickChase(entity, ai); break;
                    case AIState.Attack: TickAttack(entity, ai); break;
                    default:
                        break;
                }
            }
        }

        // NEW: Extracted target finding logic so both Idle and Wander can use it
        private bool TryFindPlayer(EntityInstance entity, AIInstance ai, TransformInstance transform)
        {
            var detectionShape = new CircleShape(ai.AggroRadius, isBlocking: false);

            var body = new CollisionBody(
                entity.ID,
                transform.RoomSpatialID,
                transform.Position,
                new Vector2(0, 0),
                transform.LayerZ,
                detectionShape,
                CollisionLayer.None,
                CollisionLayer.Player);

            var collision = collisionService.QueryOverlap(body, transform.Position);

            foreach (var hit in collision.Entities)
            {
                ai.TargetEntityId = hit.ID;
                return true;
            }

            return false;
        }

        private bool IsOutsideLeash(
            AIInstance ai,
            TransformInstance transform)
        {
            return ai.HomePosition != null
                && Vector2.Distance(transform.Position, ai.HomePosition) > ai.LeashDistance;
        }

        private void TickIdle(
            EntityInstance entity,
            AIInstance ai)
        {
            var transform = entity.GetComponent<TransformInstance>();
            if (transform == null)
            {
                return;
            }

            // Check for player
            if (TryFindPlayer(entity, ai, transform))
            {
                ai.AIState = AIState.Chase;
                return;
            }

            // Stand still for one think, otherwise the last wander direction keeps walking forever
            transform.ClearMovementIntent();
            ai.AIState = AIState.Wander;
        }

        private void TickWander(
            EntityInstance entity,
            AIInstance ai) // Added AIInstance parameter
        {
            var movement = entity.GetComponent<TransformInstance>();
            if (movement == null)
            {
                return;
            }

            // 1. Check for the player while wandering!
            if (TryFindPlayer(entity, ai, movement))
            {
                ai.AIState = AIState.Chase;
                return;
            }

            // 2. If no player, wander, but walk back home once outside the leash
            Vector2 direction = IsOutsideLeash(ai, movement)
                ? Vector2.Normalize(ai.HomePosition! - movement.Position)
                : Vector2.Normalize(new Vector2(Random.Shared.NextSingle() - 0.5f, Random.Shared.NextSingle() - 0.5f));

            movement.SetMovementIntent(direction);

            // 3. Return to idle so it stops and looks around next tick, rather than getting stuck in Wander
            ai.AIState = AIState.Idle;
        }

        private void TickChase(
            EntityInstance entity,
            AIInstance ai)
        {
            var movement = entity.GetComponent<TransformInstance>();
            if (movement == null || string.IsNullOrEmpty(ai.TargetEntityId))
            {
                return;
            }

            var target = worldContext.GetEntity(ai.TargetEntityId);
            var targetTransform = target?.GetComponent<TransformInstance>();
            if (target == null || targetTransform == null)
            {
                movement.ClearMovementIntent();
                ai.TargetEntityId = null;
                ai.AIState = AIState.Idle;
                return;
            }

            float distance = Vector2.Distance(movement.Position, targetTransform.Position);

            // Give up when the target escapes, or when the chase dragged the entity too far from home
            if (distance > ai.LeashDistance || IsOutsideLeash(ai, movement))
            {
                ai.TargetEntityId = null;
                ai.AIState = AIState.Idle; // Changed to Idle
                movement.ClearMovementIntent();
                return;
            }

            float attackRange = ai.AttackRange;
            if (distance <= attackRange)
            {
                ai.AIState = AIState.Attack;
                movement.ClearMovementIntent();
                return;
            }

            Vector2 moveDir = Vector2.Normalize(targetTransform.Position - movement.Position);
            movement.SetMovementIntent(moveDir);
        }

        private void TickAttack(
            EntityInstance entity,
            AIInstance ai)
        {
            var transform = entity.GetComponent<TransformInstance>();
            if (transform == null || string.IsNullOrEmpty(ai.TargetEntityId))
            {
                ai.AIState = AIState.Idle;
                return;
            }

            var target = worldContext.GetEntity(ai.TargetEntityId);
            if (target == null)
            {
                transform.ClearMovementIntent();
                ai.AIState = AIState.Idle;
                return;
            }

            var targetTransform = target.GetComponent<TransformInstance>();
            if (targetTransform == null)
            {
                transform.ClearMovementIntent();
                ai.AIState = AIState.Idle;
                return;
            }

            // 1. Stop moving when preparing/executing the attack
            transform.ClearMovementIntent();

            // 2. Target left range: chase again right away (chase handles leash and escape)
            float currentDist = Vector2.Distance(transform.Position, targetTransform.Position);
            if (currentDist > ai.AttackRange)
            {
                ai.AIState = AIState.Chase;
                return;
            }

            if (string.IsNullOrEmpty(ai.EquippedItemDefinitionID))
                return;

            var actionState = entity.GetComponent<ActionInstance>();
            var inventory = entity.GetComponent<InventoryInstance>();
            if (actionState == null || inventory == null)
                return;

            var itemInstance = inventory.Items.FirstOrDefault(i => i.DefinitionID == ai.EquippedItemDefinitionID);
            if (itemInstance == null)
                return;

            // 3. The item cooldown in ItemService is the only attack timer, so fire the moment it allows
            //    (an intent sent while on cooldown would be discarded by ItemService)
            if (actionState.IsOnCooldown(itemInstance.ID))
                return;

            actionState.SetItemUseIntent(
                itemInstance.ID,
                targetTransform.Position,
                unequippedSlot: null,
                ItemUsageAction.Use);
        }
        #endregion
    }
}