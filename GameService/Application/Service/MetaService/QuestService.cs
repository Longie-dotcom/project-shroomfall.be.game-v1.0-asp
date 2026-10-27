using Contract;
using Contract.DTO.Feature.Quest.Command;
using Contract.DTO.Runtime.MetaDomain;
using Contract.Enum.EntityDomain;
using Domain.Runtime.EntityDomain;
using Domain.Runtime.EntityDomain.Component;

namespace Application.Service.MetaService
{
    public class QuestService
    {
        #region Attributes
        private readonly Dictionary<string, QuestInstanceDTO> quests = new();
        private readonly GachaService gachaService;
        #endregion

        #region Properties
        #endregion

        public QuestService(
            GachaService gachaService)
        {
            this.gachaService = gachaService;
        }

        #region Methods
        public void StoreQuest(
            QuestInstanceDTO quest)
        {
            quest.Id = Guid.NewGuid().ToString();
            quests.Add(quest.Id, quest);
        }

        public (List<QuestInstanceDTO> Items, int TotalCount) QueryQuests(
            QuestInstanceQueryDTO query)
        {
            IEnumerable<QuestInstanceDTO> result = quests.Values;

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var searchTerm = query.SearchTerm.Trim();
                result = result.Where(x => !string.IsNullOrEmpty(x.Id) && x.Id.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            }

            if (query.Type.HasValue)
            {
                result = result.Where(x => x.Type == query.Type.Value);
            }

            var totalCount = result.Count();
            var pageNumber = Math.Max(1, query.PageNumber);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);

            var items = result
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (items, totalCount);
        }

        public QuestInstanceDTO? GetQuest(
            string questInstanceId)
        {
            return quests.GetValueOrDefault(questInstanceId);
        }

        public QuestInstanceDTO? TryAcceptQuest(
            EntityInstance player,
            string questInstanceId)
        {
            var questProgress = player.GetComponent<QuestProgressInstance>();
            if (questProgress == null)
                return null;

            // Update expired quests before checking the active quest limit.
            var now = DateTime.UtcNow;
            UpdateExpiredQuests(questProgress, now);

            // Retrieve quest definition.
            var quest = GetQuest(questInstanceId);
            if (quest == null || quest.ExpiredAt <= now)
                return null;

            // Prevent accepting the same quest instance more than once.
            if (questProgress.Entries.Any(x => x.QuestInstanceId == questInstanceId))
                return null;

            // Enforce active quest limit.
            if (questProgress.Entries.Count(x => x.Status == QuestProgressStatus.InProgress) >= Constraint.MAX_ACTIVE_QUESTS)
                return null;

            // Initialize quest-specific progress.
            IdentificationProgress? identificationProgress = null;
            if (quest.IdentificationConfig != null)
            {
                var requiredCount = quest.IdentificationConfig.RequiredDefeatCount;
                if (requiredCount <= 0)
                    return null;

                identificationProgress = new IdentificationProgress(
                    correctDefeatCount: 0,
                    requiredDefeatCount: requiredCount);
            }
            else if (quest.CategorizingConfig == null && quest.ShootingConfig == null)
            {
                // No supported quest configuration.
                return null;
            }

            // Accept the quest.
            questProgress.Entries.Add(new QuestProgressEntry(
                questInstanceId,
                now,
                QuestProgressStatus.InProgress,
                identificationProgress));

            return quest;
        }

        public bool TryClaimQuestReward(
            EntityInstance player,
            string questInstanceId)
        {
            var questProgress = player.GetComponent<QuestProgressInstance>();
            if (questProgress == null)
                return false;

            var entry = questProgress.Entries.FirstOrDefault(x => x.QuestInstanceId == questInstanceId);
            if (entry == null || entry.Status != QuestProgressStatus.Completed)
                return false;

            var quest = GetQuest(questInstanceId);
            if (quest == null)
                return false;

            // Roll the gacha and grant its reward.
            if (!gachaService.TryGrantGachaReward(player, quest.QuestRewardConfig))
                return false;

            // Consume the claim only after successful granting.
            return entry.TryUpdateStatus(QuestProgressStatus.Rewarded);
        }

        public bool TrySubmitQuestResult(
            EntityInstance player,
            string questInstanceId,
            bool isCompleted)
        {
            var questProgress = player.GetComponent<QuestProgressInstance>();
            if (questProgress == null)
                return false;

            // Lazy update
            UpdateExpiredQuests(questProgress, DateTime.UtcNow);

            // Allow in progress quest only
            var entry = questProgress.Entries.FirstOrDefault(x => x.QuestInstanceId == questInstanceId);
            if (entry == null || entry.Status != QuestProgressStatus.InProgress)
                return false;

            // Retrieve quest content
            var quest = GetQuest(questInstanceId);
            if (quest == null)
                return false;

            // Identification quests are completed by server-side progression.
            if (quest.IdentificationConfig != null)
                return false;

            // Only Categorizing and Shooting use frontend-submitted results.
            if (quest.CategorizingConfig == null && quest.ShootingConfig == null)
                return false;

            var targetStatus = isCompleted ? QuestProgressStatus.Completed : QuestProgressStatus.Failed;
            return entry.TryUpdateStatus(targetStatus);
        }

        public void HandleEntityDefeated(
            EntityInstance source,
            EntityInstance defeatedEntity)
        {
            var questProgress = source.GetComponent<QuestProgressInstance>();
            if (questProgress == null)
                return;

            // Lazy update
            UpdateExpiredQuests(questProgress, DateTime.UtcNow);

            foreach (var entry in questProgress.Entries)
            {
                // Allow in progress identification quest only
                var identificationProgress = entry.IdentificationProgress;
                if (entry.Status != QuestProgressStatus.InProgress || identificationProgress == null)
                    continue;

                // For identification quest only
                var quest = GetQuest(entry.QuestInstanceId);
                if (quest == null || quest.IdentificationConfig == null)
                    continue;

                // Check if the defeated entity matches the quest target
                if (quest.IdentificationConfig.TargetMushroomDefinitionId != defeatedEntity.DefinitionID)
                    continue;

                // Increase progress
                if (identificationProgress.IncreaseProgressAndCheckCompletion(1))
                    entry.TryUpdateStatus(QuestProgressStatus.Completed);
            }
        }

        private void UpdateExpiredQuests(
            QuestProgressInstance questProgress,
            DateTime now)
        {
            foreach (var entry in questProgress.Entries)
            {
                if (entry.Status != QuestProgressStatus.InProgress)
                    continue;

                var quest = GetQuest(entry.QuestInstanceId);
                if (quest == null || quest.ExpiredAt > now)
                    continue;

                entry.TryUpdateStatus(QuestProgressStatus.Failed);
            }
        }
        #endregion
    }
}