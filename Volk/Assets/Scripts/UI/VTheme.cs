using UnityEngine;

namespace Volk.UI
{
    public static class VTheme
    {
        // === COLOR PALETTE ===
        public static readonly Color Background = HexColor("#0A0A14");
        public static readonly Color Panel = HexColor("#1A1A2E");
        public static readonly Color PanelLight = HexColor("#252540");
        public static readonly Color Card = HexColor("#16213E");
        public static readonly Color CardHover = HexColor("#1F3060");

        // Accents
        public static readonly Color Red = HexColor("#E94560");
        public static readonly Color Gold = HexColor("#FFD700");
        public static readonly Color Blue = HexColor("#00D4FF");
        public static readonly Color Green = HexColor("#00E676");
        public static readonly Color Orange = HexColor("#FF6D00");

        // Text
        public static readonly Color TextPrimary = HexColor("#FFFFFF");
        public static readonly Color TextSecondary = HexColor("#B0B0C0");
        public static readonly Color TextMuted = HexColor("#606080");
        public static readonly Color TextGold = HexColor("#FFD700");

        // Gradients (start → end)
        public static readonly Color HPBarStart = HexColor("#E94560");
        public static readonly Color HPBarEnd = HexColor("#FF8A65");
        public static readonly Color XPBarStart = HexColor("#00D4FF");
        public static readonly Color XPBarEnd = HexColor("#7C4DFF");

        // Buttons
        public static readonly Color ButtonPrimary = HexColor("#E94560");
        public static readonly Color ButtonPrimaryHover = HexColor("#FF6B81");
        public static readonly Color ButtonSecondary = HexColor("#1A1A2E");
        public static readonly Color ButtonDisabled = HexColor("#2A2A3E");

        // === ANIMATION SETTINGS ===
        public const float ButtonPunchScaleDuration = 0.15f;
        public const float ButtonPunchScaleMin = 0.92f;
        public const float ButtonPunchScaleMax = 1.08f;
        public const float ScreenTransitionDuration = 0.3f;
        public const float FadeInDuration = 0.4f;

        // === LAYOUT ===
        public const float MinTouchTarget = 44f; // dp
        public const float TopBarHeight = 80f;
        public const float TabBarHeight = 90f;
        public const float CardCornerRadius = 16f;
        public const float ButtonCornerRadius = 12f;
        public const float PanelPadding = 20f;

        static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }
    }
}
