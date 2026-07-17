using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CubeBlaster
{
    public enum GameState { Menu, Playing, Won }

    /// <summary>
    /// Orchestrator. Scene object with serialized references to the views, prefabs and UI.
    /// Owns the VoxelModel, target reservation, level flow, win/stars/save.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Views (scene objects)")]
        [SerializeField] SculptureView sculpture;
        [SerializeField] BankArea bank;
        [SerializeField] BoardInput input;
        [SerializeField] CameraRig cameraRig;
        [SerializeField] UIController ui;

        [Header("Prefabs")]
        [SerializeField] GunSlot gunSlotPrefab;
        [SerializeField] Dart dartPrefab;

        [Header("Roots")]
        [SerializeField] Transform gunSlotRoot;
        [SerializeField] Transform dartRoot;

        readonly List<GunSlot> _slots = new List<GunSlot>();
        readonly HashSet<int> _reserved = new HashSet<int>();

        VoxelModel _model;
        LevelData _data;
        GameState _state = GameState.Menu;
        int _level = 1;
        float _levelStart;
        int _firedProgress;
        int _totalVoxels;

        public GameState State => _state;
        public int Level => _level;
        public bool AcceptingInput => _state == GameState.Playing;
        public IReadOnlyList<GunSlot> Slots => _slots;
        public int AliveVoxels => _model != null ? _model.AliveCount : 0;
        public int TotalVoxels => _totalVoxels;
        public SculptureView Sculpture => sculpture;
        public Vector3 VoxelWorldPos(int index) => sculpture != null ? sculpture.GetWorldPos(index) : Vector3.zero;

        void Start()
        {
            if (ui != null) ui.Init(this);
            if (AudioManager.Instance != null) AudioManager.Instance.StartMusic();
            ShowMainMenu();
        }

        // ---------------- flow ----------------

        public void ShowMainMenu()
        {
            _state = GameState.Menu;
            TeardownLevel();
            if (ui != null) ui.ShowMainMenu();
        }

        public void ShowLevelSelect()
        {
            _state = GameState.Menu;
            TeardownLevel();
            if (ui != null) ui.ShowLevelSelect();
        }

        public void StartLevel(int level)
        {
            TeardownLevel();
            _level = Mathf.Clamp(level, 1, Mathf.Max(1, LevelLibrary.Count));
            _data = LevelLibrary.Load(_level);

            _model = new VoxelModel(_data);
            _model.AllCleared += OnAllCleared;
            _totalVoxels = _model.Count;
            _firedProgress = 0;

            sculpture.Init(_model, _data.paletteIndex);

            BuildSlots(_data.gunSlots);
            bank.Init(_data.bank, _data.bankColors, _data.paletteIndex, _data.bankColumns);
            if (input != null) input.Init(this);
            if (cameraRig != null) cameraRig.Fit(sculpture.Bounds);

            _reserved.Clear();
            _levelStart = Time.time;
            _state = GameState.Playing;
            if (ui != null) ui.ShowHud(_level);
        }

        public void RestartLevel() => StartLevel(_level);

        public void NextLevel()
        {
            int next = _level + 1;
            if (next > LevelLibrary.Count) { ShowLevelSelect(); return; }
            StartLevel(next);
        }

        void BuildSlots(int count)
        {
            var cfg = Cfg.Active;
            for (int i = 0; i < count; i++)
            {
                var slot = Instantiate(gunSlotPrefab, gunSlotRoot != null ? gunSlotRoot : transform);
                float x = (i - (count - 1) * 0.5f) * cfg.gunSlotSpacing;
                slot.transform.position = new Vector3(x, cfg.gunSlotY, cfg.gunSlotZ);
                slot.Init(this, i);
                _slots.Add(slot);
            }
        }

        void TeardownLevel()
        {
            if (_model != null) { _model.AllCleared -= OnAllCleared; _model = null; }
            foreach (var s in _slots) if (s != null) Destroy(s.gameObject);
            _slots.Clear();
            _reserved.Clear();
            if (dartRoot != null)
                for (int i = dartRoot.childCount - 1; i >= 0; i--) Destroy(dartRoot.GetChild(i).gameObject);
        }

        // ---------------- targeting / firing ----------------

        /// <summary>
        /// Reserve a live, un-targeted voxel of the given color (top-biased) — guns are
        /// color-locked and may only destroy voxels matching their own color.
        /// Returns -1 if none available.
        /// </summary>
        public int RequestTarget(int color)
        {
            if (_model == null) return -1;

            int best = -1, bestY = int.MinValue, seen = 0;
            for (int i = 0; i < _model.Count; i++)
            {
                if (!_model.IsAlive(i) || _reserved.Contains(i)) continue;
                var c = _model.GetCell(i);
                if (c.c != color) continue;
                // Prefer highest band; reservoir within the current best band for variety.
                if (c.y > bestY) { bestY = c.y; best = i; seen = 1; }
                else if (c.y == bestY) { seen++; if (Random.value < 1f / seen) best = i; }
            }
            if (best >= 0) _reserved.Add(best);
            return best;
        }

        /// <summary>Live voxels left of a color — a gun retires once its color hits zero.</summary>
        public int AliveOfColor(int color) => _model != null ? _model.AliveCountOfColor(color) : 0;

        /// <summary>Display color for a voxel color slot in the current level (reads the baked
        /// material asset, so hand-edits to the .mat propagate to gun/dart/fx tints).</summary>
        public Color ColorOf(int colorIndex) =>
            Visuals.Active.VoxelColor(_data != null ? _data.paletteIndex : 0, colorIndex);

        public void SpawnDart(int voxelIndex, Vector3 from)
        {
            var dart = Instantiate(dartPrefab, dartRoot != null ? dartRoot : transform);
            Vector3 target = sculpture.GetWorldPos(voxelIndex);
            // Tint the dart like its target voxel (== the firing gun's color).
            dart.Init(this, voxelIndex, from, target, ColorOf(_model.GetCell(voxelIndex).c));

            if (AudioManager.Instance != null)
            {
                _firedProgress++;
                float prog = _totalVoxels > 0 ? (float)_firedProgress / _totalVoxels : 0f;
                AudioManager.Instance.PlayShoot(prog);
            }
        }

        public void ResolveDartHit(int voxelIndex, Vector3 pos)
        {
            _reserved.Remove(voxelIndex);
            if (_model == null) return;
            if (_model.Destroy(voxelIndex))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayVoxelBreak();
            }
        }

        // ---------------- deploy (from BoardInput) ----------------

        public GunSlot FirstEmptySlot()
        {
            foreach (var s in _slots) if (s != null && s.IsEmpty) return s;
            return null;
        }

        public GunSlot NearestEmptySlotOnScreen(Vector2 screenPos, float maxPx, Camera cam)
        {
            GunSlot best = null; float bestD = maxPx;
            foreach (var s in _slots)
            {
                if (s == null || !s.IsEmpty) continue;
                Vector3 sp = cam.WorldToScreenPoint(s.transform.position);
                if (sp.z < 0) continue;
                float d = Vector2.Distance(screenPos, sp);
                if (d < bestD) { bestD = d; best = s; }
            }
            return best;
        }

        public bool DeployBlock(BankBlock block, GunSlot slot)
        {
            if (block == null || slot == null || !slot.IsEmpty) return false;
            // No cube of this block's color left → deploying would just waste the block.
            if (AliveOfColor(block.ColorIndex) <= 0) return false;
            if (!slot.Deploy(block.Value, block.ColorIndex)) return false;
            bank.Consume(block);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
            return true;
        }

        // ---------------- win ----------------

        void OnAllCleared()
        {
            if (_state != GameState.Playing) return;
            _state = GameState.Won;
            StartCoroutine(WinRoutine());
        }

        IEnumerator WinRoutine()
        {
            int stars = ComputeStars();
            SaveSystem.SetStars(_level, stars);
            SaveSystem.HighestUnlocked = _level + 1;
            SaveSystem.Coins = SaveSystem.Coins + Cfg.Active.coinsPerLevel;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayWin();

            yield return new WaitForSeconds(Cfg.Active.winPopupDelay);
            bool hasNext = _level < LevelLibrary.Count;
            if (ui != null) ui.ShowWin(_level, stars, SaveSystem.Coins, hasNext);
        }

        int ComputeStars()
        {
            float par = 3f + _totalVoxels * 0.045f;
            float t = Time.time - _levelStart;
            if (t <= par * 0.6f) return 3;
            if (t <= par) return 2;
            return 1;
        }
    }
}
