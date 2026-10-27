using Contract;
using Contract.Enum.EntityDomain;
using Domain.Abstraction;

namespace Domain.Runtime.EntityDomain.Component
{
    public class QuestProgressInstance : ComponentInstance
    {
        #region Attributes
        #endregion

        #region Properties
        public List<QuestProgressEntry> Entries { get; private set; }
        #endregion

        // NOTE: This component tracking identification quest progress ONLY
        public QuestProgressInstance(
            List<QuestProgressEntry> entries) : base(Guid.Empty)
        {
            Entries = entries;
        }

        #region Methods
        #endregion
    }

    public class QuestProgressEntry
    {
        #region Attributes
        #endregion

        #region Properties
        public string QuestInstanceId { get; private set; }
        public DateTime StartedAt { get; private set; }
        public QuestProgressStatus Status { get; private set; }
        public IdentificationProgress? IdentificationProgress { get; private set; }
        #endregion

        public QuestProgressEntry(
            string questInstanceId,
            DateTime startedAt,
            QuestProgressStatus status,
            IdentificationProgress? identificationProgress)
        {
            QuestInstanceId = questInstanceId;
            StartedAt = startedAt;
            Status = status;
            IdentificationProgress = identificationProgress;
        }

        #region Methods
        public bool TryUpdateStatus(
            QuestProgressStatus status)
        {
            // State Diagram
            if (Status == QuestProgressStatus.InProgress)
            {
                if (status != QuestProgressStatus.Completed && status != QuestProgressStatus.Failed)
                    return false;
            }
            else if (Status == QuestProgressStatus.Completed)
            {
                if (status != QuestProgressStatus.Rewarded)
                    return false;
            }
            else
            {
                return false;
            }

            Status = status;
            return true;
        }
        #endregion
    }

    public class IdentificationProgress
    {
        #region Attributes
        #endregion

        #region Properties
        public int CorrectDefeatCount { get; private set; }
        public int RequiredDefeatCount { get; private set; }
        public float Percentage => RequiredDefeatCount <= 0 ? 0f : Math.Min(100f, (float)CorrectDefeatCount / RequiredDefeatCount * 100f);
        #endregion

        public IdentificationProgress(
            int correctDefeatCount,
            int requiredDefeatCount)
        {
            CorrectDefeatCount = correctDefeatCount;
            RequiredDefeatCount = requiredDefeatCount;
        }

        #region Methods
        public bool IncreaseProgressAndCheckCompletion(
            int defeatCount)
        {
            if (defeatCount <= 0 || RequiredDefeatCount <= 0)
                return false;

            CorrectDefeatCount = Math.Min(RequiredDefeatCount, CorrectDefeatCount + defeatCount);
            return CorrectDefeatCount >= RequiredDefeatCount;
        }
        #endregion
    }
}