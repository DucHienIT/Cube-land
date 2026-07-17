using UnityEngine;

namespace CubeBlaster
{
    /// <summary>
    /// Owns all screens (plain classes) and toggles them. Attached to the UICanvas scene object.
    /// Screens are rebuilt/refreshed on show so stars & unlock state stay current.
    /// </summary>
    public class UIController : MonoBehaviour
    {
        [Header("Scene-authored refs")]
        [SerializeField] RectTransform root;

        GameManager _gm;
        RectTransform _root;

        MainMenuScreen _menu;
        LevelSelectScreen _select;
        HudScreen _hud;
        WinScreen _win;
        SettingsScreen _settings;

        public GameManager Game => _gm;
        public RectTransform Root => _root;

        public void Init(GameManager gm)
        {
            _gm = gm;
            _root = root;

            _menu = new MainMenuScreen(this);
            _select = new LevelSelectScreen(this);
            _hud = new HudScreen(this);
            _win = new WinScreen(this);
            _settings = new SettingsScreen(this);

            _menu.Build(_root); _menu.Hide();
            _select.Build(_root); _select.Hide();
            _hud.Build(_root); _hud.Hide();
            _win.Build(_root); _win.Hide();
            _settings.Build(_root); _settings.Hide();
        }

        void HideAll()
        {
            _menu.Hide(); _select.Hide(); _hud.Hide(); _win.Hide();
        }

        public void ShowMainMenu() { HideAll(); _menu.Refresh(); _menu.Show(); }
        public void ShowLevelSelect() { HideAll(); _select.Refresh(); _select.Show(); }
        public void ShowHud(int level) { HideAll(); _hud.Set(level); _hud.Show(); }
        public void ShowWin(int level, int stars, int coins, bool hasNext) { _win.Set(level, stars, coins, hasNext); _win.Show(); }
        public void ToggleSettings() { if (_settings.Visible) _settings.Hide(); else { _settings.Refresh(); _settings.Show(); } }

        void Update()
        {
            if (_gm != null && _gm.State == GameState.Playing && _hud != null)
                _hud.SetProgress(_gm.AliveVoxels, _gm.TotalVoxels);
        }

        // Called by screens
        public void PlayPressed()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
            ShowLevelSelect();
        }
        public void StartLevel(int level) { if (AudioManager.Instance != null) AudioManager.Instance.PlayClick(); _gm.StartLevel(level); }
        public void BackToMenu() { if (AudioManager.Instance != null) AudioManager.Instance.PlayClick(); _gm.ShowMainMenu(); }
        public void Restart() { if (AudioManager.Instance != null) AudioManager.Instance.PlayClick(); _gm.RestartLevel(); }
        public void Next() { if (AudioManager.Instance != null) AudioManager.Instance.PlayClick(); _win.Hide(); _gm.NextLevel(); }
    }
}
