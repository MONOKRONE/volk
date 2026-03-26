using UnityEngine;
using UnityEngine.UI;

namespace Volk.UI
{
    public class SkillCooldownUI : MonoBehaviour
    {
        public Fighter fighter;
        public Image skill1Fill;
        public Image skill2Fill;

        private float display1;
        private float display2;

        void Update()
        {
            if (fighter == null) return;

            // Smooth fill animation
            display1 = Mathf.Lerp(display1, fighter.Skill1CooldownRatio, Time.deltaTime * 12f);
            display2 = Mathf.Lerp(display2, fighter.Skill2CooldownRatio, Time.deltaTime * 12f);

            if (skill1Fill != null)
                skill1Fill.fillAmount = display1;

            if (skill2Fill != null)
                skill2Fill.fillAmount = display2;
        }
    }
}
