using UnityEngine;

namespace Volk.Core
{
    [CreateAssetMenu(fileName = "NewSkill", menuName = "VOLK/Skill Data")]
    public class SkillData : ScriptableObject
    {
        public string skillName;
        public float damage = 25f;
        public float cooldown = 5f;
        public string animationTrigger;
        public GameObject vfxPrefab;
        public AudioClip sfxClip;
    }
}
