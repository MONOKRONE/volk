using UnityEngine;

namespace Volk.Cinematic
{
    /// <summary>
    /// Applies a 2.39:1 cinematic letterbox to the camera by adjusting its rect.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CinematicLetterbox : MonoBehaviour
    {
        [Header("Letterbox")]
        public float targetAspect = 2.39f;

        void OnEnable()
        {
            Apply();
        }

        void Apply()
        {
            var cam = GetComponent<Camera>();
            float screenAspect = (float)Screen.width / Screen.height;

            if (screenAspect < targetAspect)
            {
                // Pillarbox (shouldn't happen on mobile, but handle it)
                float scale = screenAspect / targetAspect;
                cam.rect = new Rect((1f - scale) / 2f, 0, scale, 1f);
            }
            else
            {
                // Letterbox
                float scale = targetAspect / screenAspect;
                cam.rect = new Rect(0, (1f - scale) / 2f, 1f, scale);
            }
        }

        void OnDisable()
        {
            var cam = GetComponent<Camera>();
            cam.rect = new Rect(0, 0, 1, 1);
        }
    }
}
