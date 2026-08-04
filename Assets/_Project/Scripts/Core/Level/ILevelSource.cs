namespace CubeBlaster
{
    public interface ILevelSource
    {
        int Count { get; }
        LevelData Load(int level);
    }
}
