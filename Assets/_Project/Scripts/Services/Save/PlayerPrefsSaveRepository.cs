using UnityEngine;

namespace CubeBlaster
{
    public sealed class PlayerPrefsSaveRepository : ISaveRepository
    {
        const string Prefix = "cubeblaster.";
        const string UnlockedKey = Prefix + "unlocked";
        const string CoinsKey = Prefix + "coins";
        const string MutedKey = Prefix + "muted";
        const string StarsKey = Prefix + "stars.";

        public int HighestUnlocked
        {
            get => Mathf.Max(1, PlayerPrefs.GetInt(UnlockedKey, 1));
            set => WriteInt(UnlockedKey, Mathf.Max(HighestUnlocked, value));
        }

        public int Coins
        {
            get => PlayerPrefs.GetInt(CoinsKey, 0);
            set => WriteInt(CoinsKey, Mathf.Max(0, value));
        }

        public bool Muted
        {
            get => PlayerPrefs.GetInt(MutedKey, 0) == 1;
            set => WriteInt(MutedKey, value ? 1 : 0);
        }

        public int GetStars(int level) => PlayerPrefs.GetInt(StarsKey + level, 0);

        public void SetStars(int level, int stars)
        {
            if (stars <= GetStars(level)) return;
            WriteInt(StarsKey + level, stars);
        }

        public void ClearAll()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }

        static void WriteInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
            PlayerPrefs.Save();
        }
    }
}
