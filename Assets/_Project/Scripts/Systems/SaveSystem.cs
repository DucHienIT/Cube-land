using UnityEngine;

namespace CubeBlaster
{
    /// <summary>Static PlayerPrefs save with a game-specific key prefix.</summary>
    public static class SaveSystem
    {
        const string P = "cubeblaster.";

        public static int HighestUnlocked
        {
            get => Mathf.Max(1, PlayerPrefs.GetInt(P + "unlocked", 1));
            set { PlayerPrefs.SetInt(P + "unlocked", Mathf.Max(HighestUnlocked, value)); PlayerPrefs.Save(); }
        }

        public static int Coins
        {
            get => PlayerPrefs.GetInt(P + "coins", 0);
            set { PlayerPrefs.SetInt(P + "coins", Mathf.Max(0, value)); PlayerPrefs.Save(); }
        }

        public static int Stars(int level) => PlayerPrefs.GetInt(P + "stars." + level, 0);

        public static void SetStars(int level, int stars)
        {
            int cur = Stars(level);
            if (stars > cur) { PlayerPrefs.SetInt(P + "stars." + level, stars); PlayerPrefs.Save(); }
        }

        public static int TotalStars
        {
            get
            {
                int total = 0;
                for (int i = 1; i <= HighestUnlocked; i++) total += Stars(i);
                return total;
            }
        }

        public static bool Muted
        {
            get => PlayerPrefs.GetInt(P + "muted", 0) == 1;
            set { PlayerPrefs.SetInt(P + "muted", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static void ClearAll()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }
    }
}
