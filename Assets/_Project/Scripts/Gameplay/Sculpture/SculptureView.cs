using UnityEngine;

namespace CubeBlaster
{
    public class SculptureView : MonoBehaviour, ISculptureSpace, ISpinnable
    {
        const float MinScale = 0.01f;
        const float PopUpBias = 0.5f;

        [SerializeField] VoxelCube voxelPrefab;

        VoxelCubeField _field;
        Turntable _turntable;
        IFxService _fx;

        VoxelModel _model;
        SculptureLayout _layout;
        float _cellSize = 1f;
        float _scale = 1f;

        public Bounds Bounds => _layout != null
            ? _layout.WorldBounds
            : new Bounds(GameConfig.Active.sculptureCenter, Vector3.one);

        void Awake()
        {
            _field = new VoxelCubeField(voxelPrefab, transform);
            _turntable = new Turntable(transform);
            _fx = FxService.Current ?? NullFxService.Instance;

            var config = GameConfig.Active;
            _turntable.Configure(TurntableSettings.From(config), config.sculptureRestYaw);
        }

        public void Initialize(VoxelModel model, int paletteIndex)
        {
            Unsubscribe();

            var config = GameConfig.Active;
            _model = model;
            _cellSize = config.voxelSize + config.voxelGap;
            _scale = Mathf.Max(MinScale, config.sculptureScale);

            transform.position = config.sculptureCenter;
            transform.localScale = Vector3.one * _scale;
            _turntable.Configure(TurntableSettings.From(config), config.sculptureRestYaw);

            _layout = new SculptureLayout(model, config, _cellSize, _scale);
            _field.Build(model, _layout, VisualLibrary.Active, paletteIndex, VoxelStyle.From(config));

            _model.VoxelDestroyed += OnVoxelDestroyed;
        }

        public Vector3 WorldToGrid(Vector3 world) =>
            transform.InverseTransformPoint(world) / _cellSize + (_layout?.GridOrigin ?? Vector3.zero);

        public Vector3 GetWorldPosition(int voxelIndex) => _field != null && _field.HasLocalPosition(voxelIndex)
            ? transform.TransformPoint(_field.LocalPosition(voxelIndex))
            : GameConfig.Active.sculptureCenter;

        public void ApplyYaw(float pixelsDeltaX) => _turntable.ApplyYaw(pixelsDeltaX);

        void OnVoxelDestroyed(int index)
        {
            var cube = _field.DetachCube(index);
            if (cube == null) return;

            var config = GameConfig.Active;
            Vector3 position = cube.transform.position;
            Vector3 outward = position - transform.position;
            outward.y = Mathf.Abs(outward.y) + PopUpBias;

            _fx.PlayImpact(position, cube.Color);
            cube.Pop(outward, PopSettings.From(config));
            _field.PunchAround(index, _cellSize, config);
        }

        void OnDestroy() => Unsubscribe();

        void Unsubscribe()
        {
            if (_model == null) return;
            _model.VoxelDestroyed -= OnVoxelDestroyed;
            _model = null;
        }
    }
}
