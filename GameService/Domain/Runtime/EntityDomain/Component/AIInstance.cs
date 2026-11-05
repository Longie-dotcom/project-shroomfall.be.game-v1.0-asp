using Contract.Common;
using Contract.Enum.EntityDomain;
using Domain.Abstraction;

namespace Domain.Runtime.EntityDomain.Component
{
    public class AIInstance : ComponentInstance
    {
        #region Attributes
        #endregion

        #region Properties
        public AIState AIState { get; set; } = AIState.Idle;
        public string? TargetEntityId { get; set; }
        public bool IsAIControlled { get; set; } = true;
        public float ThinkCooldownRemaining { get; set; }
        public float LeashDistance { get; set; } = 10.0f;
        public float AggroRadius { get; set; } = 5.0f;
        public string EquippedItemDefinitionID { get; private set; }
        public float ThinkInterval { get; set; }
        public float AttackRange { get; private set; }
        public Vector2? HomePosition { get; set; }
        #endregion

        public AIInstance(
            Guid definitionId,
            float leashDistance,
            float aggroRadius,
            bool isAIControlled,
            float thinkInterval,
            string equippedItemDefinitionId,
            float attackRange) : base(definitionId)
        {
            LeashDistance = leashDistance;
            AggroRadius = aggroRadius;
            IsAIControlled = isAIControlled;
            ThinkInterval = thinkInterval;
            ThinkCooldownRemaining = thinkInterval;
            EquippedItemDefinitionID = equippedItemDefinitionId;
            AttackRange = attackRange;
        }

        #region Methods
        #endregion
    }
}