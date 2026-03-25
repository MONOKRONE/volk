using UnityEngine;
using System;

namespace Volk.Core
{
    public enum TowerBuff
    {
        None,
        AttackBoost,      // +15% attack damage
        DefenseBoost,     // +15% defense
        SkillCooldown     // -20% skill cooldown
    }

    public class SurvivalTowerManager : MonoBehaviour
    {
        public static SurvivalTowerManager Instance { get; private set; }

        public int CurrentFloor { get; private set; }
        public int HighestFloor { get; private set; }
        public int DailyAttemptsLeft { get; private set; }
        public TowerBuff ActiveBuff { get; private set; }

        public const int MAX_FLOOR = 50;
        public const int DAILY_ATTEMPTS = 3;
        public static readonly int[] CheckpointFloors = { 10, 25, 40 };

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadState();
        }

        void LoadState()
        {
            HighestFloor = PlayerPrefs.GetInt("tower_highest", 0);
            CurrentFloor = PlayerPrefs.GetInt("tower_current", 0);
            ActiveBuff = (TowerBuff)PlayerPrefs.GetInt("tower_buff", 0);

            // Daily attempt reset
            string lastDate = PlayerPrefs.GetString("tower_last_date", "");
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (lastDate != today)
            {
                DailyAttemptsLeft = DAILY_ATTEMPTS;
                PlayerPrefs.SetString("tower_last_date", today);
                PlayerPrefs.SetInt("tower_attempts", DAILY_ATTEMPTS);
            }
            else
            {
                DailyAttemptsLeft = PlayerPrefs.GetInt("tower_attempts", DAILY_ATTEMPTS);
            }
        }

        public bool CanAttempt() => DailyAttemptsLeft > 0;

        public void StartNewRun()
        {
            if (!CanAttempt()) return;
            DailyAttemptsLeft--;
            PlayerPrefs.SetInt("tower_attempts", DailyAttemptsLeft);
            CurrentFloor = 0;
            ActiveBuff = TowerBuff.None;
            SaveState();
            AdvanceFloor();
        }

        public void ContinueFromCheckpoint()
        {
            if (!CanAttempt()) return;
            DailyAttemptsLeft--;
            PlayerPrefs.SetInt("tower_attempts", DailyAttemptsLeft);

            // Find highest checkpoint at or below current floor
            int checkpoint = 0;
            foreach (int cp in CheckpointFloors)
            {
                if (cp <= HighestFloor) checkpoint = cp;
            }
            CurrentFloor = checkpoint;
            SaveState();
            AdvanceFloor();
        }

        public void AdvanceFloor()
        {
            CurrentFloor++;
            if (CurrentFloor > HighestFloor)
                HighestFloor = CurrentFloor;

            SaveState();
            Debug.Log($"[Tower] Floor {CurrentFloor}/{MAX_FLOOR}");

            // Every 10 floors: buff selection + reward
            if (CurrentFloor % 10 == 0)
            {
                int reward = 50 * (CurrentFloor / 10);
                if (SaveManager.Instance != null)
                    SaveManager.Instance.AddCurrency(reward);
                Debug.Log($"[Tower] Milestone reward: {reward} coins");

                if (CurrentFloor == 25)
                    Debug.Log("[Tower] Equipment token reward (placeholder)");
                if (CurrentFloor == 50)
                    Debug.Log("[Tower] Exclusive skin token reward (placeholder)");
            }
        }

        public void SelectBuff(TowerBuff buff)
        {
            ActiveBuff = buff;
            PlayerPrefs.SetInt("tower_buff", (int)buff);
            PlayerPrefs.Save();
            Debug.Log($"[Tower] Buff selected: {buff}");
        }

        public void OnFloorWon()
        {
            if (CurrentFloor >= MAX_FLOOR)
            {
                Debug.Log("[Tower] Tower complete!");
                return;
            }
            AdvanceFloor();
        }

        public void OnFloorLost()
        {
            Debug.Log($"[Tower] Lost at floor {CurrentFloor}");
            // Don't reset — player can continue from checkpoint
        }

        /// <summary>
        /// Get the buff multiplier to apply to fighter stats.
        /// </summary>
        public float GetAttackMultiplier() => ActiveBuff == TowerBuff.AttackBoost ? 1.15f : 1f;
        public float GetDefenseMultiplier() => ActiveBuff == TowerBuff.DefenseBoost ? 1.15f : 1f;
        public float GetCooldownMultiplier() => ActiveBuff == TowerBuff.SkillCooldown ? 0.8f : 1f;

        /// <summary>
        /// Get AI difficulty for current floor.
        /// </summary>
        public AIDifficulty GetFloorDifficulty()
        {
            if (CurrentFloor <= 15) return AIDifficulty.Easy;
            if (CurrentFloor <= 35) return AIDifficulty.Normal;
            return AIDifficulty.Hard;
        }

        /// <summary>
        /// Get enemy HP multiplier for current floor.
        /// </summary>
        public float GetFloorHPMultiplier() => 1f + (CurrentFloor * 0.02f);

        void SaveState()
        {
            PlayerPrefs.SetInt("tower_highest", HighestFloor);
            PlayerPrefs.SetInt("tower_current", CurrentFloor);
            PlayerPrefs.Save();
        }

        public bool IsCheckpoint(int floor) => Array.IndexOf(CheckpointFloors, floor) >= 0;
        public bool IsBuffSelectionFloor => CurrentFloor > 0 && CurrentFloor % 10 == 0 && CurrentFloor < MAX_FLOOR;
    }
}
