namespace CubeBlaster
{
    public interface IGameFlow
    {
        GameState State { get; }
        int Level { get; }
        int AliveVoxels { get; }
        int TotalVoxels { get; }

        void StartLevel(int level);
        void RestartLevel();
        void NextLevel();
        void ShowMainMenu();
        void ShowLevelSelect();
    }
}
