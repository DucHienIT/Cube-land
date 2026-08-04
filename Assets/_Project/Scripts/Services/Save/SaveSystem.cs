namespace CubeBlaster
{
    public static class SaveSystem
    {
        static ISaveRepository _repository = new PlayerPrefsSaveRepository();

        public static void Use(ISaveRepository repository)
        {
            if (repository != null) _repository = repository;
        }

        public static int HighestUnlocked
        {
            get => _repository.HighestUnlocked;
            set => _repository.HighestUnlocked = value;
        }

        public static int Coins
        {
            get => _repository.Coins;
            set => _repository.Coins = value;
        }

        public static bool Muted
        {
            get => _repository.Muted;
            set => _repository.Muted = value;
        }

        public static int GetStars(int level) => _repository.GetStars(level);

        public static void SetStars(int level, int stars) => _repository.SetStars(level, stars);

        public static int TotalStars
        {
            get
            {
                int total = 0;
                for (int level = 1; level <= HighestUnlocked; level++) total += GetStars(level);
                return total;
            }
        }

        public static void ClearAll() => _repository.ClearAll();
    }
}
