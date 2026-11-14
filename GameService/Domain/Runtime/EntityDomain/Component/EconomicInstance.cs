using Domain.Abstraction;

namespace Domain.Runtime.EntityDomain.Component
{
    public class EconomicInstance : ComponentInstance
    {
        #region Attributes
        #endregion

        #region Properties
        public int CurrentMoney { get; private set; }
        #endregion

        public EconomicInstance(
            int currentMoney) : base(Guid.Empty) 
        {
            CurrentMoney = currentMoney;
        }

        #region Methods
        public void AddMoney(
            int amount)
        {
            CurrentMoney += amount;
        }

        /// <summary>
        /// Returns false (and changes nothing) when the entity can't afford it.
        /// </summary>
        public bool TrySpendMoney(
            int amount)
        {
            if (amount > CurrentMoney)
                return false;

            CurrentMoney -= amount;
            return true;
        }
        #endregion
    }
}