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

        // Player Level & XP
        public int playerLevel = 1;
        public int playerXP;

        // Character Mastery — "CHARNAME:level:xp" pairs
        public List<string> masteryData = new List<string>();

        // Survival Tower
        public int survivalHighestFloor;

        // Unlocks
        public List<string> unlockedCharacters = new List<string>();
        public List<string> discoveredCombos = new List<string>();

        // Stage progress
        public int completedStage;

        // Equipment & Achievements
        public List<string> ownedEquipment = new List<string>();
        public List<string> equippedItems = new List<string>(); // slot:itemId
        public List<string> completedAchievements = new List<string>();
        public int totalStars;

        // Monetization
        public int battlePassTier;
        public int battlePassXP;
        public bool battlePassPremium;
        public List<string> ownedCosmetics = new List<string>();
        public List<string> equippedCosmetics = new List<string>(); // type=itemId

        // Settings
        public bool soundOn = true;
        public bool vibrationOn = true;
        public int difficulty = 1; // 0=Easy, 1=Normal, 2=Hard

        // Meta
        public string lastSaveTime;
        public float totalPlayTimeSeconds;
    }
}
