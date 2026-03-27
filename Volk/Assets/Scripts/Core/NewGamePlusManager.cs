using UnityEngine;
using System.Collections.Generic;

namespace Volk.Core
{
    [System.Flags]
    public enum NGPlusModifier
    {
        None = 0,
        GlassCannon = 1 << 0,  // Both sides 25% HP
        NoHUD = 1 << 1,        // No health bars or UI
        HyperArmor = 1 << 2,   // Enemies don't stagger
    }

    public class NewGamePlusManager : MonoBehaviour
    {
        public static NewGamePlusManager Instance { get; private set; }

        public bool IsUnlocked { get; private set; }
        public bool IsActive { get; private set; }
        public NGPlusModifier ActiveModifiers { get; private set; }

        public const int REQUIRED_CHAPTERS = 8;
        public const int MAX_MODIFIERS = 3;
        public const float REWARD_BONUS_PER_MOD = 0.5f;
        public const float DIFFICULTY_MULTIPLIER = 2.0f;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadState();
        }

        void LoadState()
        {
            int completed = SaveManager.Instance != null
                ? SaveManager.Instance.Data.completedChapter
                : PlayerPrefs.GetInt("completed_chapter", 0);
            IsUnlocked = completed >= REQUIRED_CHAPTERS;
            IsActive = PlayerPrefs.GetInt("ngplus_active", 0) == 1;
            ActiveModifiers = (NGPlusModifier)PlayerPrefs.GetInt("ngplus_mods", 0);
        }

        public void StartNewGamePlus()
        {
            if (!IsUnlocked) return;
            IsActive = true;
            ActiveModifiers = NGPlusModifier.None;
            PlayerPrefs.SetInt("ngplus_active", 1);
            PlayerPrefs.SetInt("ngplus_mods", 0);
            PlayerPrefs.Save();
            Debug.Log("[NG+] New Game Plus started!");
        }

        public void ExitNewGamePlus()
        {
            IsActive = false;
            ActiveModifiers = NGPlusModifier.None;
            PlayerPrefs.SetInt("ngplus_active", 0);
            PlayerPrefs.SetInt("ngplus_mods", 0);
            PlayerPrefs.Save();
        }

        public bool ToggleModifier(NGPlusModifier mod)
        {
            if (HasModifier(mod))
            {
                ActiveModifiers &= ~mod;
                PlayerPrefs.SetInt("ngplus_mods", (int)ActiveModifiers);
                PlayerPrefs.Save();
                return false;
            }

            if (GetActiveModifierCount() >= MAX_MODIFIERS)
                return false;

            ActiveModifiers |= mod;
            PlayerPrefs.SetInt("ngplus_mods", (int)ActiveModifiers);
            PlayerPrefs.Save();
            return true;
        }

        public bool HasModifier(NGPlusModifier mod) => (ActiveModifiers & mod) != 0;

        public int GetActiveModifierCount()
        {
            int count = 0;
            int flags = (int)ActiveModifiers;
            while (flags != 0) { count += flags & 1; flags >>= 1; }
            return count;
        }

        /// <summary>
        /// Coin/reward multiplier: 1.0 + 0.5 per active modifier
        /// </summary>
        public float GetRewardMultiplier() => 1f + GetActiveModifierCount() * REWARD_BONUS_PER_MOD;

        /// <summary>
        /// HP multiplier for GlassCannon: both fighters get 25% HP
        /// </summary>
        public float GetHPMultiplier() => IsActive && HasModifier(NGPlusModifier.GlassCannon) ? 0.25f : 1f;

        /// <summary>
        /// Whether HUD should be hidden
        /// </summary>
        public bool ShouldHideHUD() => IsActive && HasModifier(NGPlusModifier.NoHUD);

        /// <summary>
        /// Whether enemies resist stagger
        /// </summary>
        public bool EnemyHasHyperArmor() => IsActive && HasModifier(NGPlusModifier.HyperArmor);

        /// <summary>
        /// Check if a chapter can be skipped (already completed in normal mode)
        /// </summary>
        public bool CanSkipChapter(int chapterIndex)
        {
            return IsActive && PlayerPrefs.GetInt($"chapter_cleared_{chapterIndex}", 0) == 1;
        }

        public void MarkChapterCleared(int chapterIndex)
        {
            PlayerPrefs.SetInt($"chapter_cleared_{chapterIndex}", 1);
            PlayerPrefs.Save();
        }
    }
}
