using Contract.Enum.EntityDomain;
using Domain.Abstraction;

namespace Domain.Snapshot.EntityDomain.Component
{
    public class QuestProgressSnapshot : ComponentSnapshot
    {
        public List<QuestProgressEntrySnapshot> Entries { get; set; } = new List<QuestProgressEntrySnapshot>();
    }

    public class QuestProgressEntrySnapshot
    {
        public string QuestInstanceId { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public QuestProgressStatus Status { get; set; }
        public IdentificationProgressSnapshot? IdentificationProgress { get; set; }
    }

    public class IdentificationProgressSnapshot
    {
        public int CorrectDefeatCount { get; set; }
        public int RequiredDefeatCount { get; set; }
        public float Percentage => RequiredDefeatCount <= 0 ? 0f : Math.Min(100f, (float)CorrectDefeatCount / RequiredDefeatCount * 100f);
    }
}