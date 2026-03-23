using UnityEngine;
using System;
using System.Collections.Generic;
using Volk.Core;

namespace Volk.Meta
{
    [Serializable]
    public class ActiveQuest
    {
        public string questName;
        public int currentProgress;
        public int targetCount;
        public int coinReward;
        public string conditionType;
        public bool completed;
        public bool claimed;
    }

    [Serializable]
    public class DailyQuestState
    {
        public string assignedDate;
        public List<ActiveQuest> quests = new List<ActiveQuest>();
    }

    public class DailyQuestManager : MonoBehaviour
    {
        public static DailyQuestManager Instance { get; private set; }

        [Header("Quest Pool")]
        public QuestData[] questPool;
        public int questsPerDay = 3;

        private const string QUEST_SAVE_KEY = "volk_daily_quests";
        public DailyQuestState State { get; private set; }

        public event Action OnQuestsUpdated;
        public event Action<ActiveQuest> OnQuestCompleted;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadOrAssignQuests();
        }

        void LoadOrAssignQuests()
        {
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string json = PlayerPrefs.GetString(QUEST_SAVE_KEY, "");

            if (!string.IsNullOrEmpty(json))
            {
                State = JsonUtility.FromJson<DailyQuestState>(json);
                if (State.assignedDate == today)
                    return; // same day, keep quests
            }

            // New day — assign new quests
            AssignNewQuests(today);
        }

        void AssignNewQuests(string date)
        {
            State = new DailyQuestState { assignedDate = date };

            if (questPool == null || questPool.Length == 0) return;

            // Shuffle and pick
            var available = new List<QuestData>(questPool);
            int count = Mathf.Min(questsPerDay, available.Count);

            for (int i = 0; i < count; i++)
            {
                int rnd = UnityEngine.Random.Range(i, available.Count);
                (available[i], available[rnd]) = (available[rnd], available[i]);

                var q = available[i];
                State.quests.Add(new ActiveQuest
                {
                    questName = q.questName,
                    currentProgress = 0,
                    targetCount = q.targetCount,
                    coinReward = q.coinReward,
                    conditionType = q.condition.ToString(),
                    completed = false,
                    claimed = false
                });
            }

            SaveState();
            OnQuestsUpdated?.Invoke();
        }

        public void ReportProgress(QuestCondition condition, int amount = 1)
        {
            if (State == null) return;

            string condStr = condition.ToString();
            foreach (var quest in State.quests)
            {
                if (quest.completed || quest.conditionType != condStr) continue;

                quest.currentProgress += amount;
                if (quest.currentProgress >= quest.targetCount)
                {
                    quest.completed = true;
                    OnQuestCompleted?.Invoke(quest);
                    Debug.Log($"[Quest] Completed: {quest.questName}!");
                }
            }

            SaveState();
            OnQuestsUpdated?.Invoke();
        }

        public bool ClaimReward(int questIndex)
        {
            if (questIndex < 0 || questIndex >= State.quests.Count) return false;
            var quest = State.quests[questIndex];
            if (!quest.completed || quest.claimed) return false;

            quest.claimed = true;
            SaveManager.Instance?.AddCurrency(quest.coinReward);
            SaveState();
            OnQuestsUpdated?.Invoke();
            Debug.Log($"[Quest] Claimed {quest.coinReward} coins for: {quest.questName}");
            return true;
        }

        public int UnclaimedCount()
        {
            int count = 0;
            if (State == null) return 0;
            foreach (var q in State.quests)
                if (q.completed && !q.claimed) count++;
            return count;
        }

        void SaveState()
        {
            string json = JsonUtility.ToJson(State);
            PlayerPrefs.SetString(QUEST_SAVE_KEY, json);
            PlayerPrefs.Save();
        }
    }
}
