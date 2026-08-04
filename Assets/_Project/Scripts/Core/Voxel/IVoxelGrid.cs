namespace CubeBlaster
{
    public interface IVoxelGrid
    {
        int Count { get; }
        int AliveCount { get; }
        bool IsAlive(int index);
        VoxelCell GetCell(int index);
        bool InBounds(int x, int y, int z);
        int GetIndexAt(int x, int y, int z);
        int CountAliveOfColor(int colorIndex);
    }
}
