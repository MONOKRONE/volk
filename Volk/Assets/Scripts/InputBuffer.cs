using UnityEngine;

namespace Volk
{
    public class InputBuffer : MonoBehaviour
    {
        const int BUFFER_SIZE = 20; // 20 frame = ~333ms at 60fps

        struct BufferedInput
        {
            public string action; // "Punch", "Kick", "Skill1", "Skill2", "Block"
            public float timestamp;
        }

        BufferedInput[] buffer = new BufferedInput[BUFFER_SIZE];
        int head = 0;

        public void RecordInput(string action)
        {
            buffer[head % BUFFER_SIZE] = new BufferedInput { action = action, timestamp = Time.unscaledTime };
            head++;
        }

        public bool ConsumeInput(string action, float windowSeconds = 0.333f)
        {
            for (int i = head - 1; i >= head - BUFFER_SIZE && i >= 0; i--)
            {
                int idx = ((i % BUFFER_SIZE) + BUFFER_SIZE) % BUFFER_SIZE;
                ref BufferedInput b = ref buffer[idx];
                if (b.action == action && Time.unscaledTime - b.timestamp <= windowSeconds)
                {
                    b.action = null; // consume
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Detect 2 taps of the same action within maxInterval seconds.
        /// </summary>
        public bool IsDoubleTap(string action, float maxInterval = 0.25f)
        {
            int count = 0;
            float newest = 0f;

            for (int i = head - 1; i >= head - BUFFER_SIZE && i >= 0; i--)
            {
                int idx = ((i % BUFFER_SIZE) + BUFFER_SIZE) % BUFFER_SIZE;
                var b = buffer[idx];
                if (b.action != action) continue;
                if (Time.unscaledTime - b.timestamp > maxInterval) break;

                if (count == 0) newest = b.timestamp;
                count++;

                if (count >= 2)
                    return true;
            }
            return false;
        }
    }
}
