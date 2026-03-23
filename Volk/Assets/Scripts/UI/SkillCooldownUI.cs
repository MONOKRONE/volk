using UnityEngine;
using UnityEngine.UI;

namespace Volk.UI
{
    public class SkillCooldownUI : MonoBehaviour
    {
        public Fighter fighter;
        public Image skill1Fill;
        public Image skill2Fill;

        void Update()
        {
            if (fighter == null) return;

            if (skill1Fill != null)
                skill1Fill.fillAmount = fighter.Skill1CooldownRatio;

            if (skill2Fill != null)
                skill2Fill.fillAmount = fighter.Skill2CooldownRatio;
        }
    }
}
