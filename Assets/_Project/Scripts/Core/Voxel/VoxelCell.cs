using UnityEngine;

namespace CubeBlaster
{
    public readonly struct VoxelCell
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;
        public readonly int ColorIndex;
        public readonly bool Alive;

        public VoxelCell(int x, int y, int z, int colorIndex, bool alive)
        {
            X = x;
            Y = y;
            Z = z;
            ColorIndex = colorIndex;
            Alive = alive;
        }

        public VoxelCell AsDead() => new VoxelCell(X, Y, Z, ColorIndex, false);

        public Vector3 GridPosition => new Vector3(X, Y, Z);

        public bool SameCoordinate(int x, int y, int z) => X == x && Y == y && Z == z;
    }
}
