using UnityEngine;

namespace Volk.Core
{
    [CreateAssetMenu(fileName = "NewCombo", menuName = "VOLK/Combo Data")]
    public class ComboData : ScriptableObject
    {
        public string comboName;
        [TextArea] public string description;
        public AttackType[] inputSequence;
        public float damageMultiplier = 1.5f;
        public string bonusAnimTrigger;
        public AudioClip bonusSfx;
        public GameObject bonusVfx;
    }
}
