namespace CubeBlaster
{
    public interface ISaveRepository
    {
        int HighestUnlocked { get; set; }
        int Coins { get; set; }
        bool Muted { get; set; }
        int GetStars(int level);
        void SetStars(int level, int stars);
        void ClearAll();
    }
}
