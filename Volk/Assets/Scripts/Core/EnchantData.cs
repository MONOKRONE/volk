using UnityEngine;

namespace Volk.Core
{
    public enum EnchantType { Lifesteal, Frenzy, Shield, Vampir }

    [CreateAssetMenu(fileName = "NewEnchant", menuName = "VOLK/Enchant")]
    public class EnchantData : ScriptableObject
    {
        public string enchantId;
        public string enchantName;
        [TextArea] public string description;
        public EnchantType type;
        public Sprite icon;
        public float effectValue;
        public Color glowColor;
    }

    public class EnchantManager : MonoBehaviour
    {
        public static EnchantManager Instance { get; private set; }

        public EnchantData[] allEnchants;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool ApplyEnchant(string equipmentId, string enchantId)
        {
            string key = $"enchant_{equipmentId}";
            PlayerPrefs.SetString(key, enchantId);
            PlayerPrefs.Save();
            return true;
        }

        public EnchantData GetEnchant(string equipmentId)
        {
            string enchantId = PlayerPrefs.GetString($"enchant_{equipmentId}", "");
            if (string.IsNullOrEmpty(enchantId)) return null;

            foreach (var e in allEnchants)
                if (e.enchantId == enchantId) return e;
            return null;
        }

        public bool HasEnchantStone(string enchantId)
        {
            return PlayerPrefs.GetInt($"enchant_stone_{enchantId}", 0) > 0;
        }

        public void AddEnchantStone(string enchantId)
        {
            int count = PlayerPrefs.GetInt($"enchant_stone_{enchantId}", 0);
            PlayerPrefs.SetInt($"enchant_stone_{enchantId}", count + 1);
            PlayerPrefs.Save();
        }

        public bool UseEnchantStone(string enchantId)
        {
            int count = PlayerPrefs.GetInt($"enchant_stone_{enchantId}", 0);
            if (count <= 0) return false;
            PlayerPrefs.SetInt($"enchant_stone_{enchantId}", count - 1);
            PlayerPrefs.Save();
            return true;
        }

        // Apply enchant effects to fighter
        public void ApplyEffects(Fighter fighter, string equipmentId)
        {
            var enchant = GetEnchant(equipmentId);
            if (enchant == null || fighter == null) return;

            switch (enchant.type)
            {
                case EnchantType.Frenzy:
                    fighter.walkSpeed *= (1f + enchant.effectValue);
                    fighter.runSpeed *= (1f + enchant.effectValue);
                    break;
                case EnchantType.Shield:
                    fighter.maxHP *= (1f + enchant.effectValue);
                    fighter.currentHP = fighter.maxHP;
                    break;
            }
        }
    }
}
