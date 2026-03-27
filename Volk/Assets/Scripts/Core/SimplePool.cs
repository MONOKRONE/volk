using UnityEngine;
using System.Collections.Generic;

namespace Volk.Core
{
    /// <summary>
    /// Lightweight object pool for VFX and other frequently spawned prefabs.
    /// Pre-warms on init, grows if needed, auto-returns after lifetime.
    /// </summary>
    public class SimplePool
    {
        private readonly GameObject prefab;
        private readonly Transform parent;
        private readonly Queue<GameObject> available = new Queue<GameObject>();

        public SimplePool(GameObject prefab, int preWarmCount, Transform parent = null)
        {
            this.prefab = prefab;
            this.parent = parent;

            for (int i = 0; i < preWarmCount; i++)
            {
                var obj = Object.Instantiate(prefab, parent);
                obj.SetActive(false);
                available.Enqueue(obj);
            }
        }

        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            GameObject obj;
            if (available.Count > 0)
            {
                obj = available.Dequeue();
                if (obj == null)
                {
                    obj = Object.Instantiate(prefab, parent);
                }
                obj.transform.SetPositionAndRotation(position, rotation);
                obj.SetActive(true);
            }
            else
            {
                obj = Object.Instantiate(prefab, position, rotation, parent);
            }
            return obj;
        }

        public void Return(GameObject obj)
        {
            if (obj == null) return;
            obj.SetActive(false);
            available.Enqueue(obj);
        }

        /// <summary>
        /// Get an object that auto-returns after lifetime seconds.
        /// Requires a MonoBehaviour to run the coroutine on.
        /// </summary>
        public GameObject GetTimed(Vector3 position, Quaternion rotation, float lifetime, MonoBehaviour runner)
        {
            var obj = Get(position, rotation);
            runner.StartCoroutine(ReturnAfterDelay(obj, lifetime));
            return obj;
        }

        private System.Collections.IEnumerator ReturnAfterDelay(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            Return(obj);
        }
    }
}
