using System;
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
        const float SettleSpeed = 8f;
        const float SettleThresholdSqr = 0.0001f;
        const float TapPunchAmount = 0.12f;
        const float TapPunchTime = 0.13f;

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
        Vector3 _baseScale;
        Coroutine _punch;

        BlockHop _hop;
        float _hopElapsed;
        float _hopDuration = -1f;
        Action _onLanded;

        public int Value { get; private set; }
        public int ColorIndex { get; private set; }
        public Vector3 Home { get; private set; }
        public bool Consumed { get; private set; }
        public int Row { get; private set; }
        public bool Flying => _hopDuration > 0f;

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

        /// The black outline is the ONLY hard signal for "this block can be played". The dim on
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

        /// Taken out of the bank queue but NOT destroyed: the lane behind it closes up straight
        /// away while this block is still in the air, which is what makes a tap feel immediate.
        /// It stops being a valid tap target the moment it detaches — the collider goes with it,
        /// so a second tap in the same frame cannot pick a block that is already spoken for.
        public void Detach()
        {
            Consumed = true;
            if (boxCollider != null) boxCollider.enabled = false;
            if (outlineRenderer != null) outlineRenderer.enabled = false;
        }

        /// Flies into `target` and runs `onLanded` on arrival. The caller is responsible for
        /// whatever the landing means — this class knows nothing about slots or guns.
        public void HopTo(Vector3 target, Camera camera, float height, float duration, Action onLanded)
        {
            if (_baseScale == Vector3.zero) _baseScale = transform.localScale;
            if (_punch != null) { StopCoroutine(_punch); _punch = null; }
            transform.localScale = _baseScale;

            _hop = new BlockHop(transform.position, target, camera, height);
            _hopElapsed = 0f;
            _hopDuration = Mathf.Max(0.05f, duration);
            _onLanded = onLanded;
            // No punch here: the takeoff stretch is part of BlockHop.SampleScale, which owns the
            // scale for the whole flight.
        }

        /// Feedback for a tap that could not be played (every slot busy, colour already cleared).
        public void RejectTap() => PunchScale(TapPunchAmount, TapPunchTime);

        public void Consume()
        {
            Consumed = true;
            Destroy(gameObject);
        }

        void Update()
        {
            if (Flying) TickHop();
            else Settle();
        }

        void TickHop()
        {
            _hopElapsed += Time.deltaTime;
            float t = _hopElapsed / _hopDuration;

            transform.position = _hop.Sample(t);
            transform.localScale = _baseScale * BlockHop.SampleScale(t);
            if (t < 1f) return;

            _hopDuration = -1f;
            var landed = _onLanded;
            _onLanded = null;
            landed?.Invoke();
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
                // The hop owns the scale while it is running — a punch that outlives the takeoff
                // would fight the landing shrink and pop the block back to full size.
                if (Flying) yield break;
                transform.localScale = _baseScale * (1f + amount * Ease.Pulse(elapsed / duration));
                yield return null;
            }
            transform.localScale = _baseScale;
            _punch = null;
        }
    }
}
