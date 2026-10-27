using Domain.Abstraction;

namespace Domain.Snapshot.EntityDomain.Component
{
    public class EconomicSnapshot : ComponentSnapshot
    {
        public int CurrentMoney { get; set; }
    }
}