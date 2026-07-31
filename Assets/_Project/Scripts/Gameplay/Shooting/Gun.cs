using UnityEngine;
using UnityEngine.Serialization;

namespace CubeBlaster
{
    public class Gun : MonoBehaviour
    {
        static readonly Vector3 RecoilSquash = new Vector3(1.05f, 0.88f, 1.05f);

        [Header("Prefab-authored visual refs")]
        [FormerlySerializedAs("body")]
        [SerializeField] Transform bodyPivot;
        [SerializeField] Transform barrelTip;
        [SerializeField] TMPro.TMP_Text label;
        [SerializeField] MeshRenderer bodyRenderer;
        [SerializeField] MeshRenderer[] partRenderers;

        readonly RendererTinter _tinter = new RendererTinter();

        IShooterContext _context;
        GunSlot _slot;
        RecoilSpring _recoil;
        int _ammo;
        int _colorIndex;
        float _cooldown;

        public int Ammo => _ammo;
        public int ColorIndex => _colorIndex;

        public void Initialize(IShooterContext context, GunSlot slot, int ammo, int colorIndex)
        {
            _context = context;
            _slot = slot;
            _ammo = ammo;
            _colorIndex = colorIndex;
            _recoil = new RecoilSpring(bodyPivot, RecoilSquash);

            ApplyTint(context != null ? context.GetColor(colorIndex) : PaletteConfig.Active.gunBody);
            RefreshLabel();
        }

        void Update()
        {
            if (_context == null || _ammo <= 0) return;

            if (_context.CountAliveOfColor(_colorIndex) == 0)
            {
                Retire();
                return;
            }

            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f) return;

            int target = _context.RequestTarget(_colorIndex);
            if (target < 0) return;

            Fire(target);
        }

        void LateUpdate() => _recoil?.Tick(Time.deltaTime);

        void Fire(int voxelIndex)
        {
            _cooldown = GameConfig.Active.gunFireInterval;

            Vector3 origin = barrelTip != null ? barrelTip.position : transform.position;
            Vector3 direction = barrelTip != null ? barrelTip.forward : transform.forward;
            _context.SpawnDart(voxelIndex, origin, direction);

            _ammo--;
            RefreshLabel();
            _recoil.Kick();

            if (_ammo <= 0) Retire();
        }

        void ApplyTint(Color color)
        {
            Color tint = ColorTools.ClampBrightness(color, GameConfig.Active.gunTintMaxValue);
            if (label != null) label.color = ColorTools.LabelInk(tint);
            _tinter.Apply(bodyRenderer, tint);
            _tinter.Apply(partRenderers, tint);
        }

        void RefreshLabel()
        {
            if (label != null) label.text = _ammo.ToString();
        }

        void Retire()
        {
            if (_slot != null) _slot.ReleaseGun(this);
            Destroy(gameObject);
        }
    }
}
