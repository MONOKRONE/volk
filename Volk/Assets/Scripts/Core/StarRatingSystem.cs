using UnityEngine;
using System;

namespace Volk.Core
{
    public class StarRatingSystem : MonoBehaviour
    {
        public static StarRatingSystem Instance { get; private set; }

        public event Action<int, int> OnStarsEarned; // chapterIndex, stars

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public int CalculateStars(bool won, float hpPercent, float matchDuration)
        {
            if (!won) return 0;

            int stars = 1; // base star for winning
            if (hpPercent >= 0.5f) stars = 2; // HP 50%+ remaining
            if (hpPercent >= 0.5f && matchDuration < 60f) stars = 3; // HP 50%+ AND under 60s

            return stars;
        }

        public void SaveChapterStars(int chapterIndex, int stars)
        {
            int existing = GetChapterStars(chapterIndex);
            if (stars > existing)
            {
                PlayerPrefs.SetInt($"chapter_{chapterIndex}_stars", stars);

                // Also save grade string for level map compatibility
                string grade = stars switch { 3 => "S", 2 => "A", 1 => "B", _ => "C" };
                PlayerPrefs.SetString($"chapter_{chapterIndex}_grade", grade);

                PlayerPrefs.Save();

                // Update total stars
                RecalculateTotalStars();
            }

            // Gem reward for 3 stars
            if (stars >= 3 && existing < 3)
            {
                CurrencyManager.Instance?.AddGems(3);
                Debug.Log("[Stars] 3 stars! +3 gems");
            }

            OnStarsEarned?.Invoke(chapterIndex, stars);
            CheckMilestones();
        }

        public int GetChapterStars(int chapterIndex)
        {
            return PlayerPrefs.GetInt($"chapter_{chapterIndex}_stars", 0);
        }

        public int GetTotalStars()
        {
            return SaveManager.Instance?.Data.totalStars ?? PlayerPrefs.GetInt("total_stars", 0);
        }

        void RecalculateTotalStars()
        {
            int total = 0;
            int zeroStreak = 0;
            for (int i = 0; i < 50; i++) // max 50 chapters
            {
                int s = PlayerPrefs.GetInt($"chapter_{i}_stars", 0);
                if (s == 0) { zeroStreak++; if (zeroStreak > 5) break; }
                else zeroStreak = 0;
                total += s;
            }

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.Data.totalStars = total;
                SaveManager.Instance.Save();
            }
            PlayerPrefs.SetInt("total_stars", total);
            PlayerPrefs.Save();
        }

        void CheckMilestones()
        {
            int total = GetTotalStars();

            // Silver box every 10 stars
            int milestoneReached = total / 10;
            int lastMilestone = PlayerPrefs.GetInt("star_milestone", 0);

            if (milestoneReached > lastMilestone)
            {
                PlayerPrefs.SetInt("star_milestone", milestoneReached);
                PlayerPrefs.Save();

                // Award Silver loot box
                if (LootBoxManager.Instance != null)
                {
                    LootBoxManager.Instance.OpenBox(LootBoxTier.Silver);
                    Debug.Log($"[Stars] Milestone {milestoneReached * 10} stars! Silver box earned!");
                }
            }
        }
    }
}
