using UnityEngine;

namespace CubeBlaster
{
    /// <summary>
    /// A draggable numbered ammo block in the bank. The number is its ammo value, its color is the
    /// voxel color the resulting gun is locked to (guns only destroy matching-color cubes).
    /// Dropping it on an empty gun slot deploys a gun with that ammo + color.
    /// The cube visual, number label and drag collider are authored in the BankBlock prefab;
    /// Init only swaps in the baked per-color material and sets the number.
    /// </summary>
    public class BankBlock : MonoBehaviour
    {
        [Header("Prefab-authored refs")]
        [SerializeField] MeshRenderer cubeRenderer;
        [SerializeField] TMPro.TMP_Text label;
        [SerializeField] BoxCollider box;

        // Collider → block registry so input code can resolve raycast hits without GetComponent.
        static readonly System.Collections.Generic.Dictionary<Collider, BankBlock> _byCollider =
            new System.Collections.Generic.Dictionary<Collider, BankBlock>();

        public static BankBlock FromCollider(Collider c)
        {
            BankBlock b;
            return c != null && _byCollider.TryGetValue(c, out b) ? b : null;
        }

        void OnEnable() { if (box != null) _byCollider[box] = this; }
        void OnDisable() { if (box != null) _byCollider.Remove(box); }

        public int Value { get; private set; }
        public int ColorIndex { get; private set; }
        public Vector3 Home { get; private set; }
        public bool Consumed { get; private set; }
        /// <summary>Stack row this block sits in; only row 0 (top of the stack) is playable.</summary>
        public int Row { get; private set; }

        Color _tint;
        float _returnT = -1f;
        Vector3 _returnFrom;

        public void Init(int value, int colorIndex, Material mat, Color tint)
        {
            Value = value;
            ColorIndex = colorIndex;
            _tint = tint;
            if (cubeRenderer != null && mat != null) cubeRenderer.sharedMaterial = mat;
            if (label != null) label.text = value.ToString();
        }

        public void SetHome(Vector3 home, bool snap)
        {
            Home = home;
            if (snap) transform.position = home;
        }

        /// <summary>
        /// Row 0 (playable) shows full color + bright number; queued rows keep their color but
        /// darken toward the backdrop with a dimmed number — reference-style queues. All rows lie
        /// flat on the slot-area plane; queued rows step back in Z (bankRowSpacing) behind row 0.
        /// The dim is a property-block tint, so the shared material assets stay untouched.
        /// </summary>
        public void SetRow(int row)
        {
            Row = row;
            bool front = row == 0;
            if (cubeRenderer != null)
            {
                Color back = Color.Lerp(_tint, new Color(0.17f, 0.22f, 0.35f), 0.42f);
                var mpb = new MaterialPropertyBlock();
                cubeRenderer.GetPropertyBlock(mpb);
                VisualLibrary.Tint(mpb, front ? _tint : back);
                cubeRenderer.SetPropertyBlock(mpb);
            }
            if (label != null)
            {
                label.gameObject.SetActive(true);
                label.color = front ? Color.white : new Color(0.92f, 0.94f, 1f, 0.6f);
            }
        }

        public void FollowTo(Vector3 worldPos)
        {
            _returnT = -1f;
            SetRow(0); // full color + bright number while dragging
            transform.position = worldPos;
        }

        public void ReturnHome()
        {
            _returnFrom = transform.position;
            _returnT = 0f;
        }

        public void Consume()
        {
            Consumed = true;
            Destroy(gameObject);
        }

        void Update()
        {
            if (_returnT >= 0f)
            {
                _returnT += Time.deltaTime * 6f;
                transform.position = Vector3.Lerp(_returnFrom, Home, Mathf.SmoothStep(0, 1, _returnT));
                if (_returnT >= 1f) _returnT = -1f;
            }
            else
            {
                // gentle settle toward home when idle
                if ((transform.position - Home).sqrMagnitude > 0.0001f)
                    transform.position = Vector3.Lerp(transform.position, Home, Time.deltaTime * 8f);
            }
        }
    }
}
