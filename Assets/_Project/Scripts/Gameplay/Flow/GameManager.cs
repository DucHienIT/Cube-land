using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CubeBlaster
{
    public class GameManager : MonoBehaviour, IGameFlow, IShooterContext, IDartContext, IBoardContext
    {
        [Header("Views (scene objects)")]
        [SerializeField] SculptureView sculpture;
        [SerializeField] BankArea bank;
        [SerializeField] BoardInput input;
        [SerializeField] CameraRig cameraRig;
        [SerializeField] UIController ui;

        [Header("Prefabs")]
        [SerializeField] GunSlot gunSlotPrefab;

        [Header("Roots")]
        [SerializeField] Transform gunSlotRoot;

        readonly List<GunSlot> _slots = new List<GunSlot>();

        DartField _darts;

        IStarRule _starRule = new TimeStarRule();
        ITargetSelector _targets;
        VoxelModel _model;
        LevelData _data;
        GameState _state = GameState.Menu;
        int _level = 1;
        float _levelStartTime;
        int _dartsFired;
        int _totalVoxels;

        public GameState State => _state;
        public int Level => _level;
        public bool AcceptingInput => _state == GameState.Playing;
        public IReadOnlyList<GunSlot> Slots => _slots;
        public int AliveVoxels => _model != null ? _model.AliveCount : 0;
        public int TotalVoxels => _totalVoxels;
        public ISpinnable Sculpture => sculpture;

        public void UseStarRule(IStarRule rule)
        {
            if (rule != null) _starRule = rule;
        }

        void Awake()
        {
            _darts = new DartField(transform);
        }

        void Start()
        {
            if (ui != null) ui.Initialize(this);
            AudioService.Current.StartMusic();
            ShowMainMenu();
        }

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
            _dartsFired = 0;

            sculpture.Initialize(_model, _data.paletteIndex);
            _darts.Configure(this, VisualLibrary.Active, _data.paletteIndex);
            _targets = new ExposedTargetSelector(_model,
                new CameraVoxelVisibility(_model, sculpture, () => CameraRig.Main));

            BuildSlots(_data.gunSlots);
            bank.Initialize(_data.bank, _data.bankColors, _data.paletteIndex, _data.bankColumns);
            if (input != null) input.Initialize(this);
            if (cameraRig != null) cameraRig.FitTo(sculpture.Frame);

            FxService.Current.Prewarm();

            _levelStartTime = Time.time;
            _state = GameState.Playing;
            if (ui != null) ui.ShowHud(_level);
        }

        public void RestartLevel() => StartLevel(_level);

        public void NextLevel()
        {
            int next = _level + 1;
            if (next > LevelLibrary.Count) ShowLevelSelect();
            else StartLevel(next);
        }

        void BuildSlots(int count)
        {
            var config = GameConfig.Active;
            var parent = gunSlotRoot != null ? gunSlotRoot : transform;

            for (int i = 0; i < count; i++)
            {
                var slot = Instantiate(gunSlotPrefab, parent);
                float x = (i - (count - 1) * 0.5f) * config.gunSlotSpacing;
                slot.transform.position = new Vector3(x, config.gunSlotY, config.gunSlotZ);
                slot.Initialize(this, i);
                _slots.Add(slot);
            }
        }

        void TeardownLevel()
        {
            if (_model != null)
            {
                _model.AllCleared -= OnAllCleared;
                _model = null;
            }
            _targets = null;

            foreach (var slot in _slots)
                if (slot != null) Destroy(slot.gameObject);
            _slots.Clear();

            _darts.Clear();
        }

        /// The darts have no Transform of their own, so nothing draws them unless this runs every
        /// frame. Update rather than LateUpdate: a dart resolves its hit as it arrives, and the
        /// destroy has to land before SculptureView submits the sculpture for this frame.
        void Update() => _darts.Tick(Time.deltaTime, GameConfig.Active);

        public int RequestTarget(int colorIndex) => _targets != null ? _targets.Reserve(colorIndex) : -1;

        public int CountAliveOfColor(int colorIndex) => _model != null ? _model.CountAliveOfColor(colorIndex) : 0;

        public Color GetColor(int colorIndex) =>
            VisualLibrary.Active.GetVoxelColor(_data != null ? _data.paletteIndex : 0, colorIndex);

        public Vector3 GetVoxelWorldPosition(int voxelIndex) =>
            sculpture != null ? sculpture.GetWorldPosition(voxelIndex) : Vector3.zero;

        public void SpawnDart(int voxelIndex, Vector3 origin, Vector3 barrelDirection)
        {
            _darts.Spawn(voxelIndex, _model.GetCell(voxelIndex).ColorIndex, origin, barrelDirection,
                sculpture.GetWorldPosition(voxelIndex), GameConfig.Active);

            _dartsFired++;
            float progress = _totalVoxels > 0 ? (float)_dartsFired / _totalVoxels : 0f;
            AudioService.Current.PlayShoot(progress);
        }

        /// Destroy BEFORE release. The selector re-queues a released voxel as a candidate, so
        /// releasing first hands it back a voxel that is about to die and leaves a dead entry in
        /// its bucket for the next shot to trip over. Releasing after means the release is a
        /// no-op for a hit that landed, and still frees the reservation for the case that
        /// matters — a dart that expired without destroying anything.
        public void ResolveDartHit(int voxelIndex, Vector3 position)
        {
            if (_model != null && _model.DestroyVoxel(voxelIndex))
                AudioService.Current.PlayVoxelBreak();
            if (_targets != null) _targets.Release(voxelIndex);
        }

        public GunSlot FindFirstEmptySlot()
        {
            foreach (var slot in _slots)
                if (slot != null && slot.IsEmpty) return slot;
            return null;
        }

        public GunSlot FindNearestEmptySlot(Vector2 screenPosition, float maxPixels, Camera camera)
        {
            if (camera == null) return null;

            GunSlot best = null;
            float bestDistance = maxPixels;
            foreach (var slot in _slots)
            {
                if (slot == null || !slot.IsEmpty) continue;
                Vector3 projected = camera.WorldToScreenPoint(slot.transform.position);
                if (projected.z < 0f) continue;

                float distance = Vector2.Distance(screenPosition, projected);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = slot;
            }
            return best;
        }

        public bool DeployBlock(BankBlock block, GunSlot slot)
        {
            if (block == null || slot == null || !slot.IsEmpty) return false;
            if (CountAliveOfColor(block.ColorIndex) <= 0) return false;
            if (!slot.Deploy(block.Value, block.ColorIndex)) return false;

            bank.Consume(block);
            AudioService.Current.PlayClick();
            return true;
        }

        void OnAllCleared()
        {
            if (_state != GameState.Playing) return;
            _state = GameState.Won;
            StartCoroutine(WinRoutine());
        }

        IEnumerator WinRoutine()
        {
            int stars = _starRule.Evaluate(_totalVoxels, Time.time - _levelStartTime);
            SaveSystem.SetStars(_level, stars);
            SaveSystem.HighestUnlocked = _level + 1;
            SaveSystem.Coins += GameConfig.Active.coinsPerLevel;
            AudioService.Current.PlayWin();

            yield return new WaitForSeconds(GameConfig.Active.winPopupDelay);

            if (ui != null) ui.ShowWin(_level, stars, SaveSystem.Coins, _level < LevelLibrary.Count);
        }
    }
}
