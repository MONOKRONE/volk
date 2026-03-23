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
        public int gems;

        // Unlocks
        public List<string> unlockedCharacters = new List<string>();
        public List<string> discoveredCombos = new List<string>();

        // Equipment & Achievements
        public List<string> ownedEquipment = new List<string>();
        public List<string> equippedItems = new List<string>(); // slot:itemId
        public List<string> completedAchievements = new List<string>();
        public int totalStars;

        // Settings
        public bool soundOn = true;
        public bool vibrationOn = true;
        public int difficulty = 1; // 0=Easy, 1=Normal, 2=Hard

        // Meta
        public string lastSaveTime;
    }
}
