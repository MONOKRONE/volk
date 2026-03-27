using UnityEngine;
using System.Collections.Generic;

namespace Volk.Core
{
    public class ComboTracker : MonoBehaviour
    {
        public static ComboTracker Instance { get; private set; }

        [Header("All Available Combos")]
        public ComboData[] allCombos;

        [Header("Settings")]
        public float comboInputWindow = 0.6f;
        public float holdThreshold = 0.3f;

        // Input tracking
        private List<ComboInput> inputHistory = new List<ComboInput>();
        private float lastInputTime;
        private Fighter trackedFighter;
        private int currentComboHits;

        // Hold tracking
        private AttackType? heldAttack;
        private float holdStartTime;

        public int CurrentComboHits => currentComboHits;
        public event System.Action<ComboData> OnComboDiscovered;
        public event System.Action<ComboData> OnComboExecuted;
        public event System.Action<int> OnComboHit; // hit count

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void SetTrackedFighter(Fighter fighter)
        {
            trackedFighter = fighter;
        }

        // --- Input Recording ---

        public void RegisterInput(AttackType type)
        {
            float now = Time.time;
            if (now - lastInputTime > comboInputWindow)
            {
                // PLA-130: Track combo drop
                if (inputHistory.Count > 0)
                    Volk.Core.PlayerBehaviorTracker.Instance?.OnComboAttempt(false);
                inputHistory.Clear();
                currentComboHits = 0;
            }

            // Determine input type
            ComboInputType inputType = ComboInputType.Tap;
            float duration = 0f;

            if (heldAttack.HasValue && heldAttack.Value == type)
            {
                duration = now - holdStartTime;
                if (duration >= holdThreshold)
                    inputType = ComboInputType.Hold;
            }

            var input = new ComboInput
            {
                attackType = type,
                inputType = inputType,
                holdDuration = duration
            };

            inputHistory.Add(input);
            lastInputTime = now;
            currentComboHits++;

            OnComboHit?.Invoke(currentComboHits);
            CheckCombos();

            // Reset hold
            heldAttack = null;
        }

        public void RegisterHoldStart(AttackType type)
        {
            heldAttack = type;
            holdStartTime = Time.time;
        }

        public void RegisterSkillCancel()
        {
            if (inputHistory.Count == 0) return;

            float now = Time.time;
            if (now - lastInputTime > comboInputWindow) return;

            inputHistory.Add(new ComboInput
            {
                attackType = AttackType.Punch, // placeholder
                inputType = ComboInputType.SkillCancel,
                holdDuration = 0
            });
            lastInputTime = now;

            CheckCombos();
        }

        // --- Combo Detection ---

        void CheckCombos()
        {
            if (allCombos == null) return;

            foreach (var combo in allCombos)
            {
                if (CheckAdvancedCombo(combo) || CheckLegacyCombo(combo))
                {
                    ExecuteCombo(combo);
                    inputHistory.Clear();
                    break;
                }
            }
        }

        bool CheckLegacyCombo(ComboData combo)
        {
            if (combo.inputSequence == null || combo.inputSequence.Length == 0) return false;
            if (inputHistory.Count < combo.inputSequence.Length) return false;

            int bufferStart = inputHistory.Count - combo.inputSequence.Length;
            for (int i = 0; i < combo.inputSequence.Length; i++)
            {
                if (inputHistory[bufferStart + i].attackType != combo.inputSequence[i])
                    return false;
            }
            return true;
        }

        bool CheckAdvancedCombo(ComboData combo)
        {
            if (combo.advancedSequence == null || combo.advancedSequence.Length == 0) return false;
            if (inputHistory.Count < combo.advancedSequence.Length) return false;

            int bufferStart = inputHistory.Count - combo.advancedSequence.Length;
            for (int i = 0; i < combo.advancedSequence.Length; i++)
            {
                var expected = combo.advancedSequence[i];
                var actual = inputHistory[bufferStart + i];

                // Attack type must match (unless SkillCancel)
                if (expected.inputType != ComboInputType.SkillCancel &&
                    actual.attackType != expected.attackType)
                    return false;

                // Input type must match
                if (actual.inputType != expected.inputType)
                    return false;

                // Hold must meet minimum duration
                if (expected.inputType == ComboInputType.Hold &&
                    actual.holdDuration < expected.holdDuration * 0.8f) // 80% tolerance
                    return false;
            }
            return true;
        }

        // --- Execution ---

        void ExecuteCombo(ComboData combo)
        {
            OnComboExecuted?.Invoke(combo);
            Debug.Log($"[Combo] {combo.comboName} executed! x{combo.damageMultiplier} (tier: {combo.hitTier})");

            // Spawn tier-appropriate VFX
            if (HitEffectManager.Instance != null && trackedFighter != null)
            {
                Vector3 pos = trackedFighter.transform.position + Vector3.up * 1.2f;
                HitEffectManager.Instance.SpawnTieredEffect(pos, combo.hitTier);
            }

            // Check if first time discovered
            if (SaveManager.Instance != null && !SaveManager.Instance.Data.discoveredCombos.Contains(combo.comboName))
            {
                SaveManager.Instance.Data.discoveredCombos.Add(combo.comboName);
                SaveManager.Instance.Save();
                OnComboDiscovered?.Invoke(combo);
                Debug.Log($"[Combo] NEW COMBO DISCOVERED: {combo.comboName}!");
            }

            if (combo.bonusSfx != null)
                AudioManager.Instance?.PlayOneShot(combo.bonusSfx);

            if (combo.bonusVfx != null && trackedFighter != null)
            {
                Vector3 pos = trackedFighter.transform.position + Vector3.up * 1.2f;
                var fx = Instantiate(combo.bonusVfx, pos, Quaternion.identity);
                Destroy(fx, 3f);
            }

            // PLA-130: Track combo attempt success
            Volk.Core.PlayerBehaviorTracker.Instance?.OnComboAttempt(true);
        }

        public void ResetCombo()
        {
            inputHistory.Clear();
            currentComboHits = 0;
            heldAttack = null;
        }

        public bool IsComboDiscovered(string comboName)
        {
            return SaveManager.Instance != null && SaveManager.Instance.Data.discoveredCombos.Contains(comboName);
        }

        public int DiscoveredCount()
        {
            return SaveManager.Instance?.Data.discoveredCombos.Count ?? 0;
        }
    }
}
