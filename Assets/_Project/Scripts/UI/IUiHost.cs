using UnityEngine;

namespace CubeBlaster
{
    public interface IUIHost
    {
        RectTransform Root { get; }
        IGameFlow Game { get; }

        void GoToLevelSelect();
        void StartLevel(int level);
        void GoToMainMenu();
        void RestartLevel();
        void GoToNextLevel();
        void ToggleSettings();
    }
}
