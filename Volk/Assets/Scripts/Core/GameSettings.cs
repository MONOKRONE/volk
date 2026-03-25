using UnityEngine;

namespace Volk.Core
{
    public class GameSettings : MonoBehaviour
    {
        public static GameSettings Instance { get; private set; }

        public CharacterData selectedCharacter;
        public CharacterData enemyCharacter;
        public ArenaData selectedArena;
        public CharacterData[] allCharacters;
        public AIDifficulty selectedDifficulty = AIDifficulty.Normal;

        // Game mode
        public enum GameMode { Story, QuickFight, Survival, Training }
        public GameMode currentMode = GameMode.QuickFight;

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
