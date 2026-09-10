using Contract.Enum.MetaDomain.Effect;
using Domain.Runtime.EntityDomain;
using Domain.Runtime.EntityDomain.Component;

namespace Application.Service.MetaService
{
    public enum DeathOutcome
    {
        None,
        Entity,
        Player
    }

    public class DeathService
    {
        #region Attributes
        #endregion

        #region Properties
        #endregion

        public DeathService() { }

        #region Methods
        public DeathOutcome CheckDeath(
            EntityInstance entity,
            AttributeType vital,
            float previousValue,
            float currentValue)
        {
            if (vital != AttributeType.Health || currentValue > 0f)
                return DeathOutcome.None;

            var ownership = entity.GetComponent<OwnershipInstance>();

            // Case A: Unowned entity (e.g., wild monsters, creeps)
            if (ownership == null)
            {
                return DeathOutcome.Entity;
            }
            // Case B: Player in an active combat run
            else
            {
                return DeathOutcome.Player;
            }
        }
        #endregion
    }
}