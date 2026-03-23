using System;
using System.Collections.Generic;

namespace Volk.Core
{
    [Serializable]
    public class SaveData
    {
        // Progress
        public int completedChapter;
        public int totalWins;
        public int totalMatches;
        public int currency;

        // Unlocks
        public List<string> unlockedCharacters = new List<string>();
        public List<string> discoveredCombos = new List<string>();

        // Settings
        public bool soundOn = true;
        public bool vibrationOn = true;
        public int difficulty = 1; // 0=Easy, 1=Normal, 2=Hard

        // Meta
        public string lastSaveTime;
    }
}
