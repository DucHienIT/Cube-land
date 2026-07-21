using UnityEngine;

namespace CubeBlaster
{
    /// <summary>
    /// A deployed shooter sitting in a slot. Color-locked: only fires at voxels of its own color.
    /// Auto-fires darts at reserved voxels every fireInterval until its ammo is spent (or no voxel
    /// of its color remains), then frees its slot and self-destructs.
    /// The whole visual subtree (body, dome, barrel, ammo label) is authored in the Gun prefab —
    /// Init only tints the serialized renderers with the gun's voxel color (via property blocks,
    /// so the shared material assets are never modified).
    /// </summary>
    public class Gun : MonoBehaviour
    {
        [Header("Prefab-authored visual refs")]
        [SerializeField] Transform body;
        [SerializeField] Transform barrelTip;
        [SerializeField] TMPro.TMP_Text label;
        [SerializeField] MeshRenderer bodyRenderer;   // tinted with the gun color
        [SerializeField] MeshRenderer domeRenderer;   // gun color, lightened
        [SerializeField] MeshRenderer tubeRenderer;   // gun color, strongly lightened
        [SerializeField] MeshRenderer[] rimRenderers; // barrel base + rim: gun color, darkened

        GameManager _gm;
        GunSlot _slot;
        int _ammo;
        int _colorIndex;
        float _timer;
        Vector3 _bodyBase;

        public int Ammo => _ammo;
        public int ColorIndex => _colorIndex;

        public void Init(GameManager gm, GunSlot slot, int ammo, int colorIndex)
        {
            _gm = gm;
            _slot = slot;
            _ammo = ammo;
            _colorIndex = colorIndex;
            if (body != null) _bodyBase = body.localScale;
            ApplyTint(gm != null ? gm.ColorOf(colorIndex) : Palette.Active.gunBody);
            Refresh();
        }

        void ApplyTint(Color color)
        {
            // Reference look: canister body in the gun's color; shoulders/tube lighter, trims darker.
            // Every tint is capped: the tint goes through a MaterialPropertyBlock, which REPLACES
            // _BaseColor instead of multiplying it, so GunPart.mat cannot hold brightness headroom
            // for us. Without the cap the white color slot arrives at ~0.97 and the lighter
            // dome/tube lerps push it to pure white — under the key light + high ambient floor that
            // clipped to a featureless white blob with no visible form.
            float cap = Cfg.Active.gunTintMaxValue;
            Tint(bodyRenderer, CapValue(color, cap));
            Tint(domeRenderer, CapValue(Color.Lerp(color, Color.white, 0.16f), cap));
            Tint(tubeRenderer, CapValue(Color.Lerp(color, Color.white, 0.42f), cap));
            Color rim = Color.Lerp(color, Color.black, 0.32f);
            if (rimRenderers != null)
                foreach (var r in rimRenderers) Tint(r, rim);
        }

        /// <summary>Scales a color down so its brightest channel is at most <paramref name="max"/>,
        /// keeping hue and saturation intact (a plain Lerp toward black would wash the hue out).</summary>
        static Color CapValue(Color c, float max)
        {
            float m = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            if (m <= max || m <= 0.0001f) return c;
            float k = max / m;
            return new Color(c.r * k, c.g * k, c.b * k, c.a);
        }

        static void Tint(MeshRenderer r, Color c)
        {
            if (r == null) return;
            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            VisualLibrary.Tint(mpb, c);
            r.SetPropertyBlock(mpb);
        }

        void Refresh()
        {
            if (label != null) label.text = _ammo.ToString();
        }

        void Update()
        {
            if (_gm == null || _ammo <= 0) return;

            // Nothing of this gun's color left alive anywhere → leftover ammo is useless; free the slot.
            if (_gm.AliveOfColor(_colorIndex) == 0) { Retire(); return; }

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            int target = _gm.RequestTarget(_colorIndex);
            if (target < 0) return; // every voxel of this color is reserved or hidden behind others

            _timer = Cfg.Active.gunFireInterval;
            Vector3 from = barrelTip != null ? barrelTip.position : transform.position;
            _gm.SpawnDart(target, from);
            _ammo--;
            Refresh();

            // small recoil punch (squash)
            if (body != null) body.localScale = Vector3.Scale(_bodyBase, new Vector3(1.05f, 0.88f, 1.05f));

            if (_ammo <= 0) Retire();
        }

        void LateUpdate()
        {
            if (body != null)
                body.localScale = Vector3.Lerp(body.localScale, _bodyBase, Time.deltaTime * 12f);
        }

        void Retire()
        {
            if (_slot != null) _slot.OnGunEmpty(this);
            Destroy(gameObject);
        }
    }
}
