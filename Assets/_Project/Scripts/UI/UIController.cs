using System.Collections.Generic;
using UnityEngine;

namespace CubeBlaster
{
    public class UIController : MonoBehaviour, IUIHost
    {
        [Header("Scene-authored refs")]
        [SerializeField] RectTransform root;

        readonly List<IUIScreen> _exclusiveScreens = new List<IUIScreen>();
        readonly List<IUIScreen> _allScreens = new List<IUIScreen>();

        bool _landscape;

        IGameFlow _game;
        MainMenuScreen _menu;
        LevelSelectScreen _select;
        HudScreen _hud;
        WinScreen _win;
        SettingsScreen _settings;

        public RectTransform Root => root;
        public IGameFlow Game => _game;

        public void Initialize(IGameFlow game)
        {
            _game = game;

            _menu = new MainMenuScreen(this);
            _select = new LevelSelectScreen(this);
            _hud = new HudScreen(this);
            _win = new WinScreen(this);
            _settings = new SettingsScreen(this);

            _exclusiveScreens.Clear();
            _exclusiveScreens.AddRange(new IUIScreen[] { _menu, _select, _hud, _win });

            _landscape = ScreenLayout.IsLandscape;
            BuildHidden(_menu, _select, _hud, _win, _settings);
        }

        void BuildHidden(params IUIScreen[] screens)
        {
            _allScreens.Clear();
            foreach (var screen in screens)
            {
                screen.Build(root);
                screen.Hide();
                _allScreens.Add(screen);
            }
        }

        /// The player owns the window size on WebGL, so the orientation is not a build-time fact:
        /// a browser resize (or a phone rotating) has to move the screens over to the other
        /// layout. Screens keep their own rects, so this is a flag compare per frame and real
        /// work only on the flip.
        void SyncOrientation()
        {
            bool landscape = ScreenLayout.IsLandscape;
            if (landscape == _landscape) return;
            _landscape = landscape;
            foreach (var screen in _allScreens) screen.ApplyLayout(landscape);
        }

        void HideExclusive()
        {
            foreach (var screen in _exclusiveScreens) screen.Hide();
        }

        public void ShowMainMenu()
        {
            HideExclusive();
            _menu.Refresh();
            _menu.Show();
        }

        public void ShowLevelSelect()
        {
            HideExclusive();
            _select.Refresh();
            _select.Show();
        }

        public void ShowHud(int level)
        {
            HideExclusive();
            _hud.Set(level);
            _hud.Show();
        }

        public void ShowWin(int level, int stars, int coins, bool hasNext)
        {
            _win.Set(level, stars, coins, hasNext);
            _win.Show();
        }

        public void ToggleSettings()
        {
            if (_settings.Visible)
            {
                _settings.Hide();
                return;
            }
            _settings.Refresh();
            _settings.Show();
        }

        void Update()
        {
            SyncOrientation();
            if (_game == null || _game.State != GameState.Playing || _hud == null) return;
            _hud.SetProgress(_game.AliveVoxels, _game.TotalVoxels);
        }

        public void GoToLevelSelect()
        {
            AudioService.Current.PlayClick();
            ShowLevelSelect();
        }

        public void StartLevel(int level)
        {
            AudioService.Current.PlayClick();
            _game.StartLevel(level);
        }

        public void GoToMainMenu()
        {
            AudioService.Current.PlayClick();
            _game.ShowMainMenu();
        }

        public void RestartLevel()
        {
            AudioService.Current.PlayClick();
            _game.RestartLevel();
        }

        public void GoToNextLevel()
        {
            AudioService.Current.PlayClick();
            _win.Hide();
            _game.NextLevel();
        }
    }
}
