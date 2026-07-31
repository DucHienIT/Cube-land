namespace CubeBlaster
{
    public interface ITargetSelector
    {
        int Reserve(int colorIndex);
        void Release(int index);
        void Clear();
    }
}
