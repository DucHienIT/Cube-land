using UnityEngine;

namespace CubeBlaster
{
    public class GunSlot : MonoBehaviour
    {
        static readonly Color IdleRim = new Color(0.085f, 0.125f, 0.215f, 1f);
        const float ActiveRimBlend = 0.34f;

        [SerializeField] Gun gunPrefab;
        [Header("Prefab-authored visual refs")]
        [SerializeField] MeshRenderer[] padRenderers;

        readonly RendererTinter _tinter = new RendererTinter();

        IShooterContext _context;
        Gun _gun;
        int _index;

        public bool IsEmpty => _gun == null;
        public int Index => _index;

        public void Initialize(IShooterContext context, int index)
        {
            _context = context;
            _index = index;
            SetRim(IdleRim);
        }

        public bool Deploy(int ammo, int colorIndex)
        {
            if (!IsEmpty) return false;

            _gun = Instantiate(gunPrefab, transform);
            _gun.transform.localPosition = Vector3.zero;
            _gun.Initialize(_context, this, ammo, colorIndex);

            Color gunColor = _context != null ? _context.GetColor(colorIndex) : Color.white;
            SetRim(Color.Lerp(IdleRim, gunColor, ActiveRimBlend));
            return true;
        }

        public void ReleaseGun(Gun gun)
        {
            if (_gun != gun) return;
            _gun = null;
            SetRim(IdleRim);
        }

        void SetRim(Color color) => _tinter.Apply(padRenderers, color);
    }
}
