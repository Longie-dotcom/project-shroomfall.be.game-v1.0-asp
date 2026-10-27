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
        #endregion
    }
}