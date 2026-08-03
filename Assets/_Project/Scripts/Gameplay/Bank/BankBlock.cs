using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace CubeBlaster
{
    public class BankBlock : MonoBehaviour
    {
        static readonly ColliderRegistry<BankBlock> Registry = new ColliderRegistry<BankBlock>();
        static readonly Color QueuedTint = new Color(0.17f, 0.22f, 0.35f);

        const float QueuedDim = 0.42f;
        const float ReturnSpeed = 6f;
        const float SettleSpeed = 8f;
        const float SettleThresholdSqr = 0.0001f;
        const float PickupPunchAmount = 0.12f;
        const float PickupPunchTime = 0.13f;

        [Header("Prefab-authored refs")]
        [SerializeField] MeshRenderer cubeRenderer;
        [SerializeField] TMPro.TMP_Text label;
        [FormerlySerializedAs("box")]
        [SerializeField] BoxCollider boxCollider;
        [Tooltip("Inverted-hull shell that marks the block as playable. Baked by " +
                 "VisualAssetBaker; a prefab from before that bake simply has no outline.")]
        [SerializeField] MeshRenderer outlineRenderer;

        readonly RendererTinter _tinter = new RendererTinter();

        Color _tint;
        float _returnProgress = -1f;
        Vector3 _returnFrom;
        Vector3 _baseScale;
        Coroutine _punch;
        bool _picked;

        public int Value { get; private set; }
        public int ColorIndex { get; private set; }
        public Vector3 Home { get; private set; }
        public bool Consumed { get; private set; }
        public int Row { get; private set; }

        public static BankBlock FindByCollider(Collider collider) => Registry.Find(collider);

        void OnEnable() => Registry.Register(boxCollider, this);

        void OnDisable() => Registry.Unregister(boxCollider);

        public void Initialize(int value, int colorIndex, Material material, Color tint)
        {
            if (_baseScale == Vector3.zero) _baseScale = transform.localScale;
            Value = value;
            ColorIndex = colorIndex;
            _tint = tint;
            if (cubeRenderer != null && material != null) cubeRenderer.sharedMaterial = material;
            if (label != null) label.text = value.ToString();
        }

        public void SetHome(Vector3 home, bool snap)
        {
            Home = home;
            if (snap) transform.position = home;
        }

        /// The black outline is the ONLY hard signal for "this block can be dragged". The dim on
        /// the queued rows says "not yet" but says nothing about which row is the live one — on a
        /// dark palette two dimmed rows read much alike — and the outline is what the reference
        /// genre uses for exactly this. It is an inverted hull rather than a screen-space edge
        /// pass so it costs one extra draw per front-row block and nothing on the queued ones.
        public void SetRow(int row)
        {
            Row = row;
            bool playable = row == 0;
            Color shown = playable ? _tint : Color.Lerp(_tint, QueuedTint, QueuedDim);

            if (outlineRenderer != null) outlineRenderer.enabled = playable;
            _tinter.Apply(cubeRenderer, shown);
            if (label == null) return;

            label.gameObject.SetActive(true);
            label.color = ColorTools.LabelInk;
        }

        public void PunchScale(float amount, float duration)
        {
            if (amount <= 0f || duration <= 0f) return;
            if (_punch != null) StopCoroutine(_punch);
            _punch = StartCoroutine(PunchRoutine(amount, duration));
        }

        public void FollowTo(Vector3 worldPosition)
        {
            if (_returnProgress >= 0f || !_picked)
            {
                _picked = true;
                PunchScale(PickupPunchAmount, PickupPunchTime);
            }
            _returnProgress = -1f;
            SetRow(0);
            transform.position = worldPosition;
        }

        public void ReturnHome()
        {
            _picked = false;
            _returnFrom = transform.position;
            _returnProgress = 0f;
        }

        public void Consume()
        {
            Consumed = true;
            Destroy(gameObject);
        }

        void Update()
        {
            if (_returnProgress >= 0f) TickReturn();
            else Settle();
        }

        void TickReturn()
        {
            _returnProgress += Time.deltaTime * ReturnSpeed;
            transform.position = Vector3.Lerp(_returnFrom, Home, Ease.SmoothStep01(_returnProgress));
            if (_returnProgress >= 1f) _returnProgress = -1f;
        }

        void Settle()
        {
            if ((transform.position - Home).sqrMagnitude <= SettleThresholdSqr) return;
            transform.position = Vector3.Lerp(transform.position, Home, Time.deltaTime * SettleSpeed);
        }

        IEnumerator PunchRoutine(float amount, float duration)
        {
            if (_baseScale == Vector3.zero) _baseScale = transform.localScale;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.localScale = _baseScale * (1f + amount * Ease.Pulse(elapsed / duration));
                yield return null;
            }
            transform.localScale = _baseScale;
            _punch = null;
        }
    }
}
