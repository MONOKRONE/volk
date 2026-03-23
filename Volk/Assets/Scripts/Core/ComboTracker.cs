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

        private List<AttackType> inputBuffer = new List<AttackType>();
        private float lastInputTime;
        private Fighter trackedFighter;

        public event System.Action<ComboData> OnComboDiscovered;
        public event System.Action<ComboData> OnComboExecuted;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void SetTrackedFighter(Fighter fighter)
        {
            trackedFighter = fighter;
        }

        public void RegisterInput(AttackType type)
        {
            float now = Time.time;
            if (now - lastInputTime > comboInputWindow)
                inputBuffer.Clear();

            inputBuffer.Add(type);
            lastInputTime = now;

            CheckCombos();
        }

        void CheckCombos()
        {
            if (allCombos == null) return;

            foreach (var combo in allCombos)
            {
                if (combo.inputSequence == null || combo.inputSequence.Length == 0) continue;
                if (inputBuffer.Count < combo.inputSequence.Length) continue;

                // Check if buffer ends with combo sequence
                bool match = true;
                int bufferStart = inputBuffer.Count - combo.inputSequence.Length;
                for (int i = 0; i < combo.inputSequence.Length; i++)
                {
                    if (inputBuffer[bufferStart + i] != combo.inputSequence[i])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    ExecuteCombo(combo);
                    inputBuffer.Clear();
                    break;
                }
            }
        }

        void ExecuteCombo(ComboData combo)
        {
            OnComboExecuted?.Invoke(combo);
            Debug.Log($"[Combo] {combo.comboName} executed! x{combo.damageMultiplier}");

            // Check if first time discovered
            if (SaveManager.Instance != null && !SaveManager.Instance.Data.discoveredCombos.Contains(combo.comboName))
            {
                SaveManager.Instance.Data.discoveredCombos.Add(combo.comboName);
                SaveManager.Instance.Save();
                OnComboDiscovered?.Invoke(combo);
                Debug.Log($"[Combo] NEW COMBO DISCOVERED: {combo.comboName}!");
            }

            // Play bonus effects
            if (combo.bonusSfx != null)
                AudioManager.Instance?.PlayOneShot(combo.bonusSfx);

            if (combo.bonusVfx != null && trackedFighter != null)
            {
                Vector3 pos = trackedFighter.transform.position + Vector3.up * 1.2f;
                Instantiate(combo.bonusVfx, pos, Quaternion.identity);
            }
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
