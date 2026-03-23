using UnityEngine;

namespace Volk.Core
{
    public class GameSettings : MonoBehaviour
    {
        public static GameSettings Instance { get; private set; }

        public CharacterData selectedCharacter;
        public CharacterData enemyCharacter;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
