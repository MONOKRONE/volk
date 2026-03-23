using UnityEngine;
using UnityEngine.UI;

namespace Volk.UI
{
    [RequireComponent(typeof(Image))]
    public class VPanel : MonoBehaviour
    {
        public enum PanelType { Background, Panel, Card, CardLight }

        public PanelType panelType = PanelType.Panel;

        void Awake()
        {
            var img = GetComponent<Image>();
            img.color = panelType switch
            {
                PanelType.Background => VTheme.Background,
                PanelType.Panel => VTheme.Panel,
                PanelType.Card => VTheme.Card,
                PanelType.CardLight => VTheme.PanelLight,
                _ => VTheme.Panel
            };
        }
    }
}
